using Core.Application.Management;
using Core.Application.Security;
using FluentValidation;
using Moq;
namespace UnitTests;
public sealed class UserManagementServiceTests
{
    private readonly Mock<IUserManagementStore> store=new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> actor=new(MockBehavior.Strict);
    private readonly Guid id=Guid.NewGuid(),actorId=Guid.NewGuid();
    private ManagedUser Existing => new(id,"operator","op@example.test","Operator",true,false,["Employee"],[],[],"version-1");
    private UserWriteRequest Request => new("operator","op@example.test","Operator","Employee",[],[],true,null,"version-1");
    private UserManagementService Service => new(store.Object,actor.Object);
    public UserManagementServiceTests()
    {
        actor.SetupGet(a=>a.UserId).Returns(actorId);
        store.Setup(s=>s.ExecuteWriteAsync(It.IsAny<Func<Task<ManagedUser>>>(),It.IsAny<CancellationToken>())).Returns((Func<Task<ManagedUser>> a,CancellationToken _)=>a());
        store.Setup(s=>s.ExecuteWriteAsync(It.IsAny<Func<Task<bool>>>(),It.IsAny<CancellationToken>())).Returns((Func<Task<bool>> a,CancellationToken _)=>a());
    }
    [Fact] public async Task WeakInitialPasswordCannotReachPersistence()
    {
        await Assert.ThrowsAsync<ValidationException>(()=>Service.CreateAsync(Request with { InitialPassword="short" }));
        store.Verify(s=>s.CreateAsync(It.IsAny<UserWriteRequest>(),It.IsAny<CancellationToken>()),Times.Never);
    }
    [Fact] public async Task ValidCreationNormalizesIdentityAndValidatesAssignments()
    {
        store.Setup(s=>s.ValidateReferencesAsync(It.IsAny<UserWriteRequest>(),It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        store.Setup(s=>s.CreateAsync(It.IsAny<UserWriteRequest>(),It.IsAny<CancellationToken>())).Returns((UserWriteRequest q,CancellationToken _)=>Task.FromResult(Existing with {Username=q.Username,FullName=q.FullName}));
        var r=await Service.CreateAsync(Request with {Username=" operator ",FullName=" Operator ",InitialPassword="Strong123!Pass"});Assert.Equal("operator",r.Username);
        store.Verify(s=>s.ValidateReferencesAsync(It.IsAny<UserWriteRequest>(),It.IsAny<CancellationToken>()),Times.Once);
    }
    [Fact] public async Task StaleVersionCannotOverwriteAnAccount()
    {
        store.Setup(s=>s.FindAsync(id,It.IsAny<CancellationToken>())).ReturnsAsync(Existing);
        await Assert.ThrowsAsync<ConflictException>(()=>Service.UpdateAsync(id,Request with {ExpectedVersion="stale"}));
        store.Verify(s=>s.UpdateAsync(id,It.IsAny<UserWriteRequest>(),It.IsAny<CancellationToken>()),Times.Never);
    }
    [Theory][InlineData(false,"Admin")][InlineData(true,"Employee")]
    public async Task OwnAccountCannotBeDisabledOrDemoted(bool active,string role)
    {
        actor.SetupGet(a=>a.UserId).Returns(id);store.Setup(s=>s.FindAsync(id,It.IsAny<CancellationToken>())).ReturnsAsync(Existing with {Roles=["Admin"]});
        await Assert.ThrowsAsync<ConflictException>(()=>Service.UpdateAsync(id,Request with {Role=role,IsActive=active}));
        store.Verify(s=>s.UpdateAsync(id,It.IsAny<UserWriteRequest>(),It.IsAny<CancellationToken>()),Times.Never);
    }
    [Fact] public async Task LastActiveAdministratorCannotBeRemoved()
    {
        store.Setup(s=>s.FindAsync(id,It.IsAny<CancellationToken>())).ReturnsAsync(Existing with {Roles=["Admin"]});store.Setup(s=>s.CountActiveAdministratorsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        await Assert.ThrowsAsync<ConflictException>(()=>Service.UpdateAsync(id,Request));
        store.Verify(s=>s.UpdateAsync(id,It.IsAny<UserWriteRequest>(),It.IsAny<CancellationToken>()),Times.Never);
    }
    [Fact] public async Task SystemSuperuserCannotBeChanged()
    {
        store.Setup(s=>s.FindAsync(id,It.IsAny<CancellationToken>())).ReturnsAsync(Existing with {IsSuperuser=true});
        await Assert.ThrowsAsync<ConflictException>(()=>Service.UpdateAsync(id,Request));
    }
    [Fact] public async Task AccountUpdatePreservesExplicitAssignments()
    {
        var farm=Guid.NewGuid(); var q=Request with {FarmIds=[farm],DirectPermissions=["weights.create"]};
        store.Setup(s=>s.FindAsync(id,It.IsAny<CancellationToken>())).ReturnsAsync(Existing);
        store.Setup(s=>s.ValidateReferencesAsync(q,It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        store.Setup(s=>s.UpdateAsync(id,q,It.IsAny<CancellationToken>())).ReturnsAsync(Existing with {FarmIds=[farm],DirectPermissions=["weights.create"]});
        var result=await Service.UpdateAsync(id,q);Assert.Equal(farm,Assert.Single(result.FarmIds));Assert.Equal("weights.create",Assert.Single(result.DirectPermissions));
    }
    [Fact] public async Task DuplicateFarmAssignmentsAreRejectedWithoutWriting()
    {
        var farm=Guid.NewGuid();await Assert.ThrowsAsync<ValidationException>(()=>Service.CreateAsync(Request with {FarmIds=[farm,farm],InitialPassword="Strong123!Pass"}));
        store.Verify(s=>s.CreateAsync(It.IsAny<UserWriteRequest>(),It.IsAny<CancellationToken>()),Times.Never);
    }
    [Fact] public async Task ResetPasswordRequiresCurrentVersionAndCallsStoreOnce()
    {
        store.Setup(s=>s.FindAsync(id,It.IsAny<CancellationToken>())).ReturnsAsync(Existing);
        var q=new PasswordResetRequest("NewPassword123!","version-1");store.Setup(s=>s.ResetPasswordAsync(id,q,It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        Assert.True(await Service.ResetPasswordAsync(id,q));store.Verify(s=>s.ResetPasswordAsync(id,q,It.IsAny<CancellationToken>()),Times.Once);
    }
}

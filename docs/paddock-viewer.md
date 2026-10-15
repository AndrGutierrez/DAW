# Isometric paddock viewer

The paddock and transfer screens render an interactive 2D farm using PixiJS 8.22.0 and pixi-viewport 6.0.3. Original SVG artwork supplies the cow frames, shelter and trees. React owns the forms, permissions and API state; PixiJS owns the scene and camera. The renderer loads only when a farm contains paddocks.

## Behavior

- Configured map rectangles keep their coordinates. Automatic slots use a fixed grid ordered by name and ID. Adding a configured rectangle preserves every unaffected automatic slot and relocates only blocked slots. The editor, active viewer and transfer map calculate geometry from the same complete farm catalog before filtering visible or eligible paddocks, so inactive geometry still reserves space. This derived layout never updates stored coordinates.
- The ground grows around the displayed paddocks, including overflow slots. The animated renderer and SVG fallback frame that same ground, keeping paddocks inside the terrain when centering.
- A few cow sprites represent each herd. Occupancy, capacity, lot membership and movement history come from the existing API, not the number of sprites.
- Selecting a paddock on the canvas or with its keyboard-accessible button opens its residents, or selects the transfer destination.
- A transfer draft projects both departures and arrivals. Animals already in the destination do not count as arrivals; lot-only changes retain the physical location.
- Dashed routes illustrate the draft. Walking travellers appear only after the existing movement endpoint confirms the saved operation. Capacity conflicts and uncertain responses retain the existing guarded form and idempotent retry.
- Pan, wheel zoom, pinch zoom, zoom buttons and keyboard camera controls are supported. Arrow keys pan, `+`/`-` zoom and `R` centers the scene.
- The animation pause control and `prefers-reduced-motion` stop cow and transfer animation. Hidden tabs stop the scene ticker. The ticker is capped at 30 FPS.
- Initialization errors or a lost WebGL context leave a static SVG map and equivalent accessible selection buttons. Graphics can be retried without reloading the business form.
- Refreshing occupancy preserves the running scene. Farm changes filter out the previous farm's snapshot while the new request completes.

## Implementation

| Responsibility | Source |
| --- | --- |
| Accessible UI, lazy loading and fallback | `src/frontend/src/components/PaddockPlan.tsx` |
| Visual editing and schematic placement | `src/frontend/src/components/PaddockEditor.tsx`, `PaddockLayout.tsx` |
| Rotation status and estimate | `src/frontend/src/api/paddocks.ts`, `src/frontend/src/components/PaddockCondition.tsx` |
| Isometric coordinates, herd grouping and projected counts | `src/frontend/src/components/paddock-scene/model.ts` |
| Original cow frames and scenery | `src/frontend/src/components/paddock-scene/art.ts` |
| Rendering, depth sorting, camera, animation and cleanup | `src/frontend/src/components/paddock-scene/renderer.ts` |
| Individual confirmed-movement callback | `src/frontend/src/components/AnimalLocation.tsx` |
| Paddock residents and visual movement confirmation | `src/frontend/src/pages/PaddocksPage.tsx` |
| Batch draft, group destination and confirmed animation | `src/frontend/src/pages/TransfersPage.tsx` |

The terrain is a schematic fixed-perspective view, not a survey or GPS map. Routes are illustrative passages between gates; they are not computed geographic routes or proof of physical traversability. The shelter and trees are decorative. No new database tables or movement endpoints are introduced.

The renderer imports PixiJS's `unsafe-eval` compatibility module, which provides synchronization polyfills that **avoid** dynamic evaluation despite the module name. Nginx keeps `script-src 'self'`; the application does not enable CSP's `unsafe-eval`. Browser tests verify both the live WebGL scene and this response header. Chromium's software WebGL flag is configured only for the automated browser runner.

## Visual workflows

The editor starts with the current farm and a suggested location away from both stored and automatic paddock positions when space permits. Existing paddocks appear as red hatched areas with names, including provisional automatic positions. The editor reads the permission-scoped paddock catalog so inactive paddocks also reserve their stored geometry; it filters the selected farm and labels inactive areas. Transfer destinations remain limited to active paddocks. The draft turns red on overlap, and the warning names the affected paddocks. Editing excludes the current paddock after computing the farm layout so other automatic positions stay consistent. Automatic positions remain schematic and are never saved by this preview. Drag the draft, click the terrain to place it, drag its corner to resize it, or use keyboard/buttons. Coordinates stay inside the existing 100 × 100 model. Advanced numeric coordinates and automatic layout remain available. Drawing dimensions do not change recorded hectares. Editing management fields on an existing unmapped paddock preserves automatic placement. Overlap produces an advisory; the drawing is schematic.

Transfers use four explicit stages: **Origin → Animals → Destination → Review**. Desktop keeps a compact map beside one active working panel. The operator advances with the panel footer or returns to an available stage with the progress control. Selecting a checkbox does not advance the workflow. Back preserves the group, destination, lot and reason; deliberately changing the origin clears the old group. Selecting the whole farm allows groups from several paddocks.

Origin selection filters residents on the server. The animal list has its own bounded scrolling area, pagination and a selection summary. Destination selection previews the route and resulting occupancy. Review lists the exact group, farm, date, lot, reason and capacity before a single final confirmation; it replaces the extra confirmation dialog. Paddock details still link directly to group or individual transfers. The map remains mounted across stages and animates movement only after the server acknowledges the saved batch.

On mobile, the animal stage focuses on the list; origin and destination retain the map, and review uses a smaller read-only preview. Stage changes move focus to the active panel. Transitions respect reduced-motion preferences. A lost submission response locks draft editing and replays the captured batch with identical identifiers, even if refreshed capacity would no longer accept a new batch. Atomic batches, expected origins and the existing server validations remain authoritative.

The optional `paddockId` filter on `GET /api/animals/page` is applied inside the accessible-farm query before counting and pagination. It composes with farm, lot, status and search filters without widening permissions.

## Estimated grazing state

The terrain uses the **configured rotation window**, not a forage-consumption simulation:

- An occupied paddock needs a valid oldest arrival, no residents with unknown arrival dates, and a positive maximum stay.
- Window usage is `min(1, days since oldest arrival / maxStayDays)`. Under 80% is the initial period; 80% up to the configured limit means rotation is approaching; reaching the limit means review rotation. Remaining days are clamped to zero.
- Green terrain and more grass strokes progress to muted ochre and fewer strokes with this window. Labels and meters describe time usage, never a percentage of remaining grass.
- Missing dates or limits produce **no estimate**. Empty paddocks show no active grazing; they are not automatically marked recovered.
- Capacity governs movement guards and occupancy. Hectares provide descriptive animals/ha. Neither is converted into forage biomass or a consumption rate. No recovery duration, rainfall, pasture yield or animal intake is invented.

This is a proxy based on the oldest current resident, not an average residence or accumulated animal-days. Partial arrivals need real recorded movements to support an estimate. Visual placement and rotation parameters share the existing paddock save request; no database migration is needed.

## Verification

```bash
npm ci --prefix src/frontend
npm run build --prefix src/frontend
npm run test:coverage --prefix src/frontend
```

Component tests cover fallback selection, projected capacity, locking during confirmation, camera controls, animation pause and disposal after an interrupted initialization. Model tests cover preservation of stored geometry, automatic placement, arrival/departure grouping, projection and animation endpoints.

Wizard component tests additionally cover explicit navigation, retaining the draft when going back, clearing selection on an origin change, guarded review, restarting after success and locked idempotent retry. On a busy Windows host, run coverage and browser tests sequentially; the verified coverage command uses `npm run test:coverage --prefix src/frontend -- --maxWorkers=2 --testTimeout=15000` to bound worker contention. The browser case that audits and captures all four stages allows 60 seconds without relaxing its assertions.

Run the browser tests against a **disposable** PostgreSQL stack with the existing seeded administrator and employee. Never enable the mutation fixture flag against a shared or production database:

```bash
export E2E_BASE_URL=http://127.0.0.1:18086
export E2E_ISOLATED_DATABASE=1
# Supply E2E_ADMIN_USERNAME / E2E_ADMIN_PASSWORD and the employee credentials privately.
npm run test:e2e --prefix src/frontend -- paddock-viewer.spec.ts paddocks.spec.ts transfers.spec.ts phase5.spec.ts
```

`paddock-viewer.spec.ts` adds desktop/mobile checks for actual pixel animation and pause, accessible controls, delayed server acknowledgement, refused movements, reduced motion and failed graphics loading. Existing movement tests continue checking real saved locations, history, capacity, concurrency and retry. A successful frontend fixture rendering alone does not verify the PostgreSQL movement contract.

The existing CI also enforces the strict 120,000,000-byte Docker image budget using uncompressed layers. Run `bash scripts/verify-image-size.sh <api-image> <frontend-image>` after building the stack; adding a graphics library does not waive that gate.

### Local verification (2026-10-14)

| Check | Result |
| --- | --- |
| Production frontend build | Passed as part of the Docker build |
| Frontend unit/component coverage run | 227 tests passed in 35 files |
| Desktop/mobile regression (viewer, paddocks, transfers and Phase 5) | 52 passed in one final run, including repeated SPA navigation |
| Targeted backend query/validator checks | 14 passed, including source pagination and farm isolation |
| Newman HTTP contracts against disposable PostgreSQL | 16 requests and 16 assertions; zero failures |
| API image, uncompressed layers | 68,464,640 bytes |
| SPA image, uncompressed layers | 75,468,800 bytes |
| Primary stack smoke check | All four transfer stages at 1536×1100 and 412×915; WebGL ready, zero movement writes, no JavaScript page errors or horizontal overflow |
| Primary data check before/after wizard redeployment | 8 animals and 4 inventory records in both snapshots |

The latest wizard deployment recreated only Nginx, leaving the primary API and PostgreSQL containers running. The authenticated smoke check traversed origin, group selection, destination and final review without confirming a transfer. Earlier paddock smoke checks exercised selection and camera controls; movement mutations ran only against the disposable stack. The matching primary record counts are a preservation check, not a byte-for-byte database comparison. Nginx retained `script-src 'self'` without `unsafe-eval`. These are local results, not a GitHub Actions execution. Private environment files and generated evidence remain excluded from Git.

The final main-stack smoke also opens an unsaved creation preview and checks that a direct farm URL retains its selected farm after reloading. On the local data checked that day, Norte and Sur had capacity and hectares, but no maximum stay and no known arrival dates for their 4 and 3 residents. They therefore correctly show no estimate. These missing records were not fabricated or backfilled.

The date-dependent browser rendering case controls the read-only snapshot dates and configured window; it is not evidence of historical movement rows. Other browser cases use real PostgreSQL writes to verify saved positions, individual/group destinations, arrival history, refused movements, retry and capacity.

## Integrated browser audit and corrections (2026-10-14)

A real unsaved draft exposed a native HTML validation failure: pointer placement produced more than four decimals, while both numeric controls and the API accept four. Visual movement and resizing now operate in integer ten-thousandths, then serialize bounded four-decimal values. The minimum size matches the existing 0.0001 contract; moving an existing small rectangle does not enlarge it. The draft named Patata was preserved and restored with valid rounded coordinates without submitting it.

The audit also reproduced a SPA navigation crash after opening a transfer from paddock residents. Pixi destroys application plugins before the stage; destroying the ticker first left Viewport trying to remove its callback from an already destroyed ticker. The renderer now stops the application, detaches and destroys Viewport while the ticker exists, then destroys the application and textures. An idempotent cleanup handles normal navigation, initialization failures and textures that finish loading after failure. Repeated navigation through real application links is now a browser regression, rather than relying on full document loads.

Pointer interaction prevents accidental SVG text selection. Transfer transitions animate displacement while preserving full text opacity, keeping contrast readable during the transition. Multi-farm browser fixtures explicitly select their farm before QR lookup, weighing and transfers.

The integrated-browser run used disposable PostgreSQL records to create a paddock by clicking an arbitrary terrain point, dragging, resizing and saving; reload and edit showed the persisted geometry. It also saved automatic placement, edited an inactive rectangle to width 0.5 and height 0.0001, moved it without enlarging it, rejected a full destination, retained a transfer draft when going back, committed a two-animal group, then moved only AUDIT-A individually. Read-only API verification confirmed the final locations and history totals of three entries for AUDIT-A and two for AUDIT-B. The primary application received only the rebuilt Nginx frontend, and its draft was not submitted.

The final automated run passed 227 frontend tests and all 52 desktop/mobile cases. Creation regression checks native validity after each click, drag and resize and compares persisted coordinates with the form. The mobile creation case also uses touchscreen tap. Other cases cover capacity, atomic rollback, concurrent arrivals, lost-response replay, role restrictions, estimated rotation scenarios, reduced motion, graphics fallback, camera controls and actual animation/pause. Rotation-stage snapshots with controlled dates verify rendering; they are not invented historical database evidence. These local checks do not replace a subsequent GitHub Actions run.


## Stable saved positions (2026-10-15)

The previous automatic layout started beyond the rightmost configured rectangle. Saving a mapped paddock near X100 therefore shifted every automatic paddock, while the fixed ground stopped at X120. The new layout reserves unaffected canonical slots first, then moves only slots blocked by configured geometry. Saved coordinates are preserved without updates or backfills. The editor, active scene and transfer destinations calculate layout from the complete farm catalog before selecting visible records, so inactive geometry does not make their positions disagree.

Both Pixi and the SVG fallback derive terrain from the displayed rectangles with equal margins. The camera includes that terrain when centering, including automatic overflow rows. Model and component regressions cover the saved Patata coordinates, relocation without cascading changes, response ordering, inactive geometry, current occupancy, overflow terrain and catalog refresh after saves. A browser regression compares editor polygons with the saved scene and again after reload.

After resuming from a workstation power interruption, all 236 frontend tests in 36 files passed, followed by TypeScript compilation. The existing production image built from this source was deployed by recreating only Nginx. The primary API and database container IDs remained identical. Before/after checks compared the full three-paddock catalog and confirmed unchanged totals of eight animals and four inventory records. Authenticated desktop and mobile smoke checks observed a ready WebGL scene, zero JavaScript page errors, no horizontal mobile overflow and zero paddock or movement writes. The integrated browser was reloaded and its Center control checked with Norte, Sur and Patata visible inside the terrain.

The strict uncompressed image measurements remained API 68,464,640 bytes and SPA 75,468,800 bytes. These are local checks, not a new GitHub Actions run. The prior interrupted validation also recorded an intermittent QR image decoding failure; the unchanged QR test subsequently passed on desktop and mobile in a focused run. That result does not constitute a QR decoder fix.

The final resumed browser run passed all 54 desktop/mobile cases in five minutes with retries disabled, including the editor/save/reload position regression, real saved movements, capacity and concurrency guards, uncertain-response replay, graphics fallback and SPA navigation. The previously failing QR case also passed unchanged in this complete run; the earlier intermittent failure remains recorded above. The disposable PostgreSQL stack was then removed after checking its Compose ownership, while the primary application remained healthy.

Integration with the current main branch retains its newer QR test: it observes the lazy decoder request without an artificial delay, uses a 600-pixel QR image and asserts the exact decoded value within 20 seconds. The explicit farm selection from this feature is retained. The earlier cold-load results above describe the pre-integration test.


## Monitoring regression alignment (2026-10-15)

The complete GitHub browser run exposed a legacy monitoring assertion that still required SVG classes and a rectangle inside a paddock button. The monitoring case now waits for the animated canvas, checks capacity and estimated grazing through the current control, selects with Enter and verifies the resident panel. Invalid geometry rejection and mobile overflow checks remain. A separate forced-fallback case compares the persisted rectangle with independently specified projected polygon vertices, then repeats the check after a full reload. It does not use the production projection function to calculate its expected result.

Validation against the current backend and PostgreSQL 15 passed the four focused desktop/mobile cases, followed by the complete unfiltered browser suite: `npm run test:e2e -- --max-failures=1`, 191 passed and one expected desktop skip for the mobile-only navigation test, in 6.8 minutes with retries disabled. This is local evidence; the new GitHub Actions run must be checked separately.

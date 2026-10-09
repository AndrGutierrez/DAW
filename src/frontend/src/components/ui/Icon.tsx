import { ArchiveIcon } from '@phosphor-icons/react/dist/csr/Archive';
import { ClockCounterClockwiseIcon } from '@phosphor-icons/react/dist/csr/ClockCounterClockwise';
import { ArrowsClockwiseIcon } from '@phosphor-icons/react/dist/csr/ArrowsClockwise';
import { PackageIcon } from '@phosphor-icons/react/dist/csr/Package';
import { FileTextIcon } from '@phosphor-icons/react/dist/csr/FileText';
import { LeafIcon } from '@phosphor-icons/react/dist/csr/Leaf';
import { CowIcon } from '@phosphor-icons/react/dist/csr/Cow';
import { ScalesIcon } from '@phosphor-icons/react/dist/csr/Scales';
import { FarmIcon } from '@phosphor-icons/react/dist/csr/Farm';
import { UserCircleIcon } from '@phosphor-icons/react/dist/csr/UserCircle';
import { MoonIcon } from '@phosphor-icons/react/dist/csr/Moon';
import { SunIcon } from '@phosphor-icons/react/dist/csr/Sun';
import { PlusIcon } from '@phosphor-icons/react/dist/csr/Plus';
import { MagnifyingGlassIcon } from '@phosphor-icons/react/dist/csr/MagnifyingGlass';
import { ArrowRightIcon } from '@phosphor-icons/react/dist/csr/ArrowRight';
import { ArrowLeftIcon } from '@phosphor-icons/react/dist/csr/ArrowLeft';
import { XIcon } from '@phosphor-icons/react/dist/csr/X';
import { CheckCircleIcon } from '@phosphor-icons/react/dist/csr/CheckCircle';
import { WarningCircleIcon } from '@phosphor-icons/react/dist/csr/WarningCircle';
import { HeartIcon } from '@phosphor-icons/react/dist/csr/Heart';
import { DropIcon } from '@phosphor-icons/react/dist/csr/Drop';
import { TreeStructureIcon } from '@phosphor-icons/react/dist/csr/TreeStructure';
import { ImagesIcon } from '@phosphor-icons/react/dist/csr/Images';
import { PencilSimpleIcon } from '@phosphor-icons/react/dist/csr/PencilSimple';
import { MapPinIcon } from '@phosphor-icons/react/dist/csr/MapPin';
import { ChartLineUpIcon } from '@phosphor-icons/react/dist/csr/ChartLineUp';
import { SignOutIcon } from '@phosphor-icons/react/dist/csr/SignOut';
import { EyeIcon } from '@phosphor-icons/react/dist/csr/Eye';
import { EyeSlashIcon } from '@phosphor-icons/react/dist/csr/EyeSlash';
import { CaretDownIcon } from '@phosphor-icons/react/dist/csr/CaretDown';
import { LockKeyIcon } from '@phosphor-icons/react/dist/csr/LockKey';
import { InfoIcon } from '@phosphor-icons/react/dist/csr/Info';
import { CircleNotchIcon } from '@phosphor-icons/react/dist/csr/CircleNotch';
import { DotsThreeIcon } from '@phosphor-icons/react/dist/csr/DotsThree';
import type { IconProps } from '@phosphor-icons/react';

const icons = {
  archive: ArchiveIcon,
  audit: ClockCounterClockwiseIcon,
  refresh: ArrowsClockwiseIcon,
  menu: DotsThreeIcon,
  inventory: PackageIcon,
  report: FileTextIcon,
  leaf: LeafIcon,
  animal: CowIcon,
  scale: ScalesIcon,
  paddock: FarmIcon,
  user: UserCircleIcon,
  moon: MoonIcon,
  sun: SunIcon,
  plus: PlusIcon,
  search: MagnifyingGlassIcon,
  arrow: ArrowRightIcon,
  back: ArrowLeftIcon,
  close: XIcon,
  check: CheckCircleIcon,
  alert: WarningCircleIcon,
  heart: HeartIcon,
  droplet: DropIcon,
  genealogy: TreeStructureIcon,
  photo: ImagesIcon,
  edit: PencilSimpleIcon,
  location: MapPinIcon,
  growth: ChartLineUpIcon,
  logout: SignOutIcon,
  eye: EyeIcon,
  eyeOff: EyeSlashIcon,
  chevron: CaretDownIcon,
  lock: LockKeyIcon,
  info: InfoIcon,
  spinner: CircleNotchIcon,
} as const;
export type IconName = keyof typeof icons;
export function Icon({ name, size = 20, ...props }: Omit<IconProps, 'size'> & { name: IconName; size?: number }) {
  const Component = icons[name];
  return <Component size={size} weight="regular" aria-hidden="true" focusable="false" {...props} />;
}

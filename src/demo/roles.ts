// Lists the sample identities available at demo sign-in.
import type { UserRole } from '@/lib/types/api';

export const demoRoles = {
  Storekeeper: 'Storekeeper',
  ProductionManager: 'Production Manager',
  LabTech: 'Lab Technician',
  QualityManager: 'Quality Manager',
  GeneralManager: 'General Manager',
  SuperAdmin: 'System Administrator',
} satisfies Record<UserRole, string>;

export function isDemoRole(value: unknown): value is UserRole {
  return typeof value === 'string' && Object.hasOwnProperty.call(demoRoles, value);
}

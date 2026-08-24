export const PERMISSIONS = {
  charactersCreate: 'characters.create',
  charactersUpdate: 'characters.update',
  charactersDelete: 'characters.delete',
} as const

export function hasPermission(permissions: string[], code: string): boolean {
  return permissions.includes(code)
}
export const PERMISSIONS = {
  charactersCreate: 'characters.create',
  charactersUpdate: 'characters.update',
  charactersDelete: 'characters.delete',
  usersCreate: 'users.create',
  usersDelete: 'users.delete',
} as const

export function hasPermission(permissions: string[], code: string): boolean {
  return permissions.includes(code)
}

// bunlar react hook değil düz fonksiyon
import { createContext, useContext } from 'react'

type PermissionContextValue = {
  permissions: string[]
  userId: string | null
}

const PermissionContext = createContext<PermissionContextValue | null>(null)

export function usePermissions(): string[] {
  const ctx = useContext(PermissionContext)
  if (!ctx) {
    throw new Error('usePermissions yalnızca AppLayout içinde')
  }
  return ctx.permissions
}

export function useUserId(): string | null {
  const ctx = useContext(PermissionContext)
  if (!ctx) {
    throw new Error('useUserId yalnızca AppLayout içinde')
  }
  return ctx.userId
}

export { PermissionContext }

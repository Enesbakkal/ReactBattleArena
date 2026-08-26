import { createContext, useContext } from 'react'

type PermissionContextValue = {
    permissions: string[]
}

const PermissionContext = createContext<PermissionContextValue | null>(null)

export function usePermissions(): string[] {
    const ctx = useContext(PermissionContext)
    if(!ctx){
        throw new Error('usePermissions yalnızca AppLayout içinde')
    }
    return ctx.permissions
}

export { PermissionContext }
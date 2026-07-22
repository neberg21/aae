import type { RouteObject } from 'react-router-dom'
import { agentsRoutes } from './agents/routes'

export const moduleRoutes: RouteObject[] = [...agentsRoutes]

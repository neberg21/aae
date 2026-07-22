import type { RouteObject } from 'react-router-dom'
import AgentDetailPage from './AgentDetailPage'
import AgentsListPage from './AgentsListPage'

export const agentsRoutes: RouteObject[] = [
  { path: '/module/agents', element: <AgentsListPage /> },
  { path: '/module/agents/:id', element: <AgentDetailPage /> },
]

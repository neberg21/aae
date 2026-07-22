import type { RouteObject } from 'react-router-dom'
import { Navigate } from 'react-router-dom'
import AgentDetailPage from './AgentDetailPage'
import AgentsListPage from './AgentsListPage'
import ThreadsPage from './ThreadsPage'

export const agentsRoutes: RouteObject[] = [
  { path: '/module/agents', element: <Navigate to="/module/agents/list" replace /> },
  { path: '/module/agents/list', element: <AgentsListPage /> },
  { path: '/module/agents/threads', element: <ThreadsPage /> },
  { path: '/module/agents/list/:id', element: <AgentDetailPage /> },
]

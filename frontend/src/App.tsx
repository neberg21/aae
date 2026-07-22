import { BrowserRouter, Navigate, useRoutes } from 'react-router-dom'
import { moduleRoutes } from './modules'

export function AppRoutes() {
  const element = useRoutes([
    { path: '/', element: <Navigate to="/module/agents" replace /> },
    ...moduleRoutes,
  ])
  return element
}

export default function App() {
  return (
    <BrowserRouter>
      <AppRoutes />
    </BrowserRouter>
  )
}

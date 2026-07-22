import { NavLink } from 'react-router-dom'

const baseLinkClass = 'rounded-md px-3 py-2 text-sm font-medium transition'

export default function ModuleNavigation() {
  return (
    <nav aria-label="Agents module navigation" className="mb-6 flex gap-2">
      <NavLink
        to="/module/agents/list"
        className={({ isActive }) =>
          isActive
            ? `${baseLinkClass} bg-violet-100 text-violet-800 dark:bg-violet-900/50 dark:text-violet-200`
            : `${baseLinkClass} text-gray-700 hover:bg-gray-100 dark:text-gray-200 dark:hover:bg-gray-800`
        }
      >
        Agents
      </NavLink>
      <NavLink
        to="/module/agents/threads"
        className={({ isActive }) =>
          isActive
            ? `${baseLinkClass} bg-violet-100 text-violet-800 dark:bg-violet-900/50 dark:text-violet-200`
            : `${baseLinkClass} text-gray-700 hover:bg-gray-100 dark:text-gray-200 dark:hover:bg-gray-800`
        }
      >
        Threads
      </NavLink>
    </nav>
  )
}


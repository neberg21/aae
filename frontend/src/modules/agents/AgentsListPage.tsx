import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { searchAgents } from './api'
import type { AgentDto } from './types'

type Status = 'idle' | 'loading' | 'success' | 'error'

export default function AgentsListPage() {
  const [name, setName] = useState('')
  const [department, setDepartment] = useState('')
  const [jobTitle, setJobTitle] = useState('')
  const [items, setItems] = useState<AgentDto[]>([])
  const [status, setStatus] = useState<Status>('idle')
  const [message, setMessage] = useState('Enter at least one filter')
  const [error, setError] = useState<string | null>(null)

  async function onSubmit(event: FormEvent) {
    event.preventDefault()

    const filters = {
      name: name.trim() || undefined,
      department: department.trim() || undefined,
      jobTitle: jobTitle.trim() || undefined,
    }

    if (!filters.name && !filters.department && !filters.jobTitle) {
      setItems([])
      setStatus('idle')
      setMessage('Enter at least one filter')
      setError(null)
      return
    }

    setStatus('loading')
    setError(null)
    try {
      const page = await searchAgents(filters)
      setItems(page.items)
      setStatus('success')
      setMessage(page.items.length === 0 ? 'No agents matched.' : '')
    } catch (err) {
      setItems([])
      setStatus('error')
      setError(err instanceof Error ? err.message : 'Search failed')
    }
  }

  return (
    <main className="mx-auto my-8 w-full max-w-4xl px-4 text-left font-sans">
      <h1 className="text-center">Agents</h1>
      <form onSubmit={onSubmit} className="mb-6 grid gap-3">
        <label className="grid gap-1 text-sm font-medium">
          Name
          <input
            className="rounded-md border border-gray-300 bg-white px-3 py-2 text-base text-gray-900 shadow-sm outline-none transition focus:border-violet-500 focus:ring-2 focus:ring-violet-200 dark:border-gray-700 dark:bg-gray-900 dark:text-gray-100 dark:focus:border-violet-400 dark:focus:ring-violet-900/40"
            value={name}
            onChange={(e) => setName(e.target.value)}
            name="name"
            autoComplete="off"
          />
        </label>
        <label className="grid gap-1 text-sm font-medium">
          Department
          <input
            className="rounded-md border border-gray-300 bg-white px-3 py-2 text-base text-gray-900 shadow-sm outline-none transition focus:border-violet-500 focus:ring-2 focus:ring-violet-200 dark:border-gray-700 dark:bg-gray-900 dark:text-gray-100 dark:focus:border-violet-400 dark:focus:ring-violet-900/40"
            value={department}
            onChange={(e) => setDepartment(e.target.value)}
            name="department"
            autoComplete="off"
          />
        </label>
        <label className="grid gap-1 text-sm font-medium">
          Job title
          <input
            className="rounded-md border border-gray-300 bg-white px-3 py-2 text-base text-gray-900 shadow-sm outline-none transition focus:border-violet-500 focus:ring-2 focus:ring-violet-200 dark:border-gray-700 dark:bg-gray-900 dark:text-gray-100 dark:focus:border-violet-400 dark:focus:ring-violet-900/40"
            value={jobTitle}
            onChange={(e) => setJobTitle(e.target.value)}
            name="jobTitle"
            autoComplete="off"
          />
        </label>
        <button
          type="submit"
          className="w-fit rounded-md bg-violet-600 px-4 py-2 font-medium text-white transition hover:bg-violet-500 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-violet-600 disabled:cursor-not-allowed disabled:opacity-60"
          disabled={status === 'loading'}
        >
          Search
        </button>
      </form>

      {status === 'loading' && <p className="text-sm text-gray-600 dark:text-gray-300">Loading...</p>}
      {error && <p role="alert" className="text-sm text-red-600 dark:text-red-400">{error}</p>}
      {status !== 'loading' && message && <p className="text-sm text-gray-600 dark:text-gray-300">{message}</p>}

      {items.length > 0 && (
        <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
          <table className="min-w-full border-collapse text-sm">
          <thead>
            <tr className="bg-gray-50 dark:bg-gray-800/60">
              <th className="border-b border-gray-200 px-3 py-2 text-left font-semibold dark:border-gray-700">Name</th>
              <th className="border-b border-gray-200 px-3 py-2 text-left font-semibold dark:border-gray-700">Department</th>
              <th className="border-b border-gray-200 px-3 py-2 text-left font-semibold dark:border-gray-700">Job title</th>
              <th className="border-b border-gray-200 px-3 py-2 text-left font-semibold dark:border-gray-700">Id</th>
            </tr>
          </thead>
          <tbody>
            {items.map((agent) => (
              <tr key={agent.agentId} className="odd:bg-white even:bg-gray-50 dark:odd:bg-transparent dark:even:bg-gray-800/30">
                <td className="border-b border-gray-200 px-3 py-2 align-top dark:border-gray-700">
                  <Link
                    to={`/module/agents/${agent.agentId}`}
                    className="text-violet-700 underline-offset-2 hover:underline dark:text-violet-300"
                  >
                    {agent.name}
                  </Link>
                </td>
                <td className="border-b border-gray-200 px-3 py-2 align-top dark:border-gray-700">{agent.department}</td>
                <td className="border-b border-gray-200 px-3 py-2 align-top dark:border-gray-700">{agent.jobTitle}</td>
                <td className="border-b border-gray-200 px-3 py-2 align-top dark:border-gray-700">{agent.agentId}</td>
              </tr>
            ))}
          </tbody>
          </table>
        </div>
      )}
    </main>
  )
}

import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ApiError, getAgent } from './api'
import type { AgentDetail } from './types'

type Status = 'loading' | 'success' | 'notfound' | 'error'

export default function AgentDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [agent, setAgent] = useState<AgentDetail | null>(null)
  const [status, setStatus] = useState<Status>('loading')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!id) {
      setStatus('notfound')
      return
    }

    let cancelled = false
    setStatus('loading')
    setError(null)

    getAgent(id)
      .then((data) => {
        if (cancelled) return
        setAgent(data)
        setStatus('success')
      })
      .catch((err: unknown) => {
        if (cancelled) return
        if (err instanceof ApiError && err.status === 404) {
          setStatus('notfound')
          setAgent(null)
          return
        }
        setStatus('error')
        setError(err instanceof Error ? err.message : 'Failed to load agent')
      })

    return () => {
      cancelled = true
    }
  }, [id])

  return (
    <main className="mx-auto my-8 w-full max-w-4xl px-4 text-left font-sans">
      <p className="mb-4">
        <Link
          to="/module/agents"
          className="text-violet-700 underline-offset-2 hover:underline dark:text-violet-300"
        >
          Back to agents
        </Link>
      </p>

      {status === 'loading' && <p className="text-sm text-gray-600 dark:text-gray-300">Loading...</p>}
      {status === 'notfound' && <p className="text-sm text-gray-600 dark:text-gray-300">Agent not found</p>}
      {status === 'error' && error && <p role="alert" className="text-sm text-red-600 dark:text-red-400">{error}</p>}

      {status === 'success' && agent && (
        <>
          <h1 className="mb-6 text-center">{agent.name}</h1>
          <dl className="grid grid-cols-[8rem_1fr] gap-x-4 gap-y-2 rounded-lg border border-gray-200 p-4 text-sm dark:border-gray-700">
            <dt className="font-semibold text-gray-700 dark:text-gray-200">Department</dt>
            <dd className="text-gray-900 dark:text-gray-100">{agent.department}</dd>
            <dt className="font-semibold text-gray-700 dark:text-gray-200">Job title</dt>
            <dd className="text-gray-900 dark:text-gray-100">{agent.jobTitle}</dd>
            <dt className="font-semibold text-gray-700 dark:text-gray-200">Id</dt>
            <dd className="text-gray-900 dark:text-gray-100">{agent.agentId}</dd>
            <dt className="font-semibold text-gray-700 dark:text-gray-200">System prompt</dt>
            <dd className="text-gray-900 dark:text-gray-100">
              <pre className="m-0 whitespace-pre-wrap rounded-md bg-gray-100 p-3 font-mono text-xs dark:bg-gray-800">{agent.systemPrompt}</pre>
            </dd>
          </dl>
        </>
      )}
    </main>
  )
}

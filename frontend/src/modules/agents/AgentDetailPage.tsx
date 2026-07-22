import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ApiError, getAgent } from './api'
import type { AgentDetail } from './types'
import './agents.css'

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
    <main className="agents-page">
      <p>
        <Link to="/module/agents">Back to agents</Link>
      </p>

      {status === 'loading' && <p>Loading…</p>}
      {status === 'notfound' && <p>Agent not found</p>}
      {status === 'error' && error && <p role="alert">{error}</p>}

      {status === 'success' && agent && (
        <>
          <h1>{agent.name}</h1>
          <dl className="agents-detail">
            <dt>Department</dt>
            <dd>{agent.department}</dd>
            <dt>Job title</dt>
            <dd>{agent.jobTitle}</dd>
            <dt>Id</dt>
            <dd>{agent.identityId}</dd>
            <dt>System prompt</dt>
            <dd>
              <pre className="agents-system-prompt">{agent.systemPrompt}</pre>
            </dd>
          </dl>
        </>
      )}
    </main>
  )
}

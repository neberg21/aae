import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { searchAgents } from './api'
import type { AgentDto } from './types'
import './agents.css'

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
    <main className="agents-page">
      <h1>Agents</h1>
      <form onSubmit={onSubmit} className="agents-filters">
        <label>
          Name
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            name="name"
            autoComplete="off"
          />
        </label>
        <label>
          Department
          <input
            value={department}
            onChange={(e) => setDepartment(e.target.value)}
            name="department"
            autoComplete="off"
          />
        </label>
        <label>
          Job title
          <input
            value={jobTitle}
            onChange={(e) => setJobTitle(e.target.value)}
            name="jobTitle"
            autoComplete="off"
          />
        </label>
        <button type="submit">Search</button>
      </form>

      {status === 'loading' && <p>Loading…</p>}
      {error && <p role="alert">{error}</p>}
      {status !== 'loading' && message && <p>{message}</p>}

      {items.length > 0 && (
        <table className="agents-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Department</th>
              <th>Job title</th>
              <th>Id</th>
            </tr>
          </thead>
          <tbody>
            {items.map((agent) => (
              <tr key={agent.agentId}>
                <td>
                  <Link to={`/module/agents/${agent.agentId}`}>{agent.name}</Link>
                </td>
                <td>{agent.department}</td>
                <td>{agent.jobTitle}</td>
                <td>{agent.agentId}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  )
}

import type { AgentDetail, AgentSearchFilters, AgentsPage } from './types'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function readJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new ApiError(response.status, response.statusText || `HTTP ${response.status}`)
  }
  return (await response.json()) as T
}

export async function searchAgents(filters: AgentSearchFilters): Promise<AgentsPage> {
  const params = new URLSearchParams()
  if (filters.name?.trim()) params.set('name', filters.name.trim())
  if (filters.department?.trim()) params.set('department', filters.department.trim())
  if (filters.jobTitle?.trim()) params.set('jobTitle', filters.jobTitle.trim())

  const query = params.toString()
  const url = query ? `/api/agents/search?${query}` : '/api/agents/search'
  const response = await fetch(url)
  return readJson<AgentsPage>(response)
}

export async function getAgent(id: string): Promise<AgentDetail> {
  const response = await fetch(`/api/agents/${encodeURIComponent(id)}`)
  return readJson<AgentDetail>(response)
}

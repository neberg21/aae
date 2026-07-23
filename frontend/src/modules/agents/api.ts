import type {
  AgentDetail,
  AgentSearchFilters,
  AgentsPage,
  ChatMessage,
  CreateVisionResponse,
  LeoChatResult,
  ThreadDetail,
  ThreadsPage,
} from './types'

const apiBaseUrl = '/ai-api'

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

function toNumber(value: number | string): number {
  if (typeof value === 'number') {
    return value
  }

  const parsed = Number.parseInt(value, 10)
  return Number.isFinite(parsed) ? parsed : 0
}

function normalizeAgentsPage(page: {
  items: AgentsPage['items']
  totalCount: number | string
  pageSize: number | string
  pageNumber: number | string
  totalPages: number | string
}): AgentsPage {
  return {
    items: page.items,
    totalCount: toNumber(page.totalCount),
    pageSize: toNumber(page.pageSize),
    pageNumber: toNumber(page.pageNumber),
    totalPages: toNumber(page.totalPages),
  }
}

function normalizeThreadsPage(page: {
  items: Array<{
    threadId: string
    createdAt: string
    updatedAt: string
    messageCount: number | string
  }>
  totalCount: number | string
  pageSize: number | string
  pageNumber: number | string
  totalPages: number | string
}): ThreadsPage {
  return {
    items: page.items.map((item) => ({
      ...item,
      messageCount: toNumber(item.messageCount),
    })),
    totalCount: toNumber(page.totalCount),
    pageSize: toNumber(page.pageSize),
    pageNumber: toNumber(page.pageNumber),
    totalPages: toNumber(page.totalPages),
  }
}

export async function getAgents(): Promise<AgentsPage> {
  const url = `${apiBaseUrl}/agents`
  const response = await fetch(url)
  const page = await readJson<{
    items: AgentsPage['items']
    totalCount: number | string
    pageSize: number | string
    pageNumber: number | string
    totalPages: number | string
  }>(response)
  return normalizeAgentsPage(page)
}

export async function searchAgents(filters: AgentSearchFilters): Promise<AgentsPage> {
  const params = new URLSearchParams()
  if (filters.name?.trim()) params.set('name', filters.name.trim())
  if (filters.department?.trim()) params.set('department', filters.department.trim())
  if (filters.jobTitle?.trim()) params.set('jobTitle', filters.jobTitle.trim())

  const query = params.toString()
  const url = query ? `${apiBaseUrl}/agents/search?${query}` : `${apiBaseUrl}/agents/search`
  const response = await fetch(url)
  const page = await readJson<{
    items: AgentsPage['items']
    totalCount: number | string
    pageSize: number | string
    pageNumber: number | string
    totalPages: number | string
  }>(response)
  return normalizeAgentsPage(page)
}

export async function getAgent(id: string): Promise<AgentDetail> {
  const response = await fetch(`${apiBaseUrl}/agents/${encodeURIComponent(id)}`)
  return readJson<AgentDetail>(response)
}

export async function getThreads(): Promise<ThreadsPage> {
  const response = await fetch(`${apiBaseUrl}/threads`)
  const page = await readJson<{
    items: Array<{
      threadId: string
      createdAt: string
      updatedAt: string
      messageCount: number | string
    }>
    totalCount: number | string
    pageSize: number | string
    pageNumber: number | string
    totalPages: number | string
  }>(response)
  return normalizeThreadsPage(page)
}

export async function getThread(threadId: string): Promise<ThreadDetail> {
  const response = await fetch(`${apiBaseUrl}/threads/${encodeURIComponent(threadId)}`)
  return readJson<ThreadDetail>(response)
}

export async function sendLeoMessage(
  message: string,
  _history: ChatMessage[] = [],
  threadId?: string,
): Promise<LeoChatResult> {
  const trimmedMessage = message.trim()
  if (!trimmedMessage) {
    throw new Error('Message is required')
  }

  const response = await fetch(`${apiBaseUrl}/chats/actions/create-vision`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      threadId: threadId ?? null,
      content: trimmedMessage,
    }),
  })

  const payload = await readJson<CreateVisionResponse>(response)
  const reply = payload.content.trim()
  if (!reply) {
    throw new Error('Leo returned an empty response')
  }

  return {
    threadId: payload.threadId,
    reply,
    done: payload.vision !== null && payload.vision !== undefined,
    vision: payload.vision ?? null,
    chatMessages: payload.chatMessages ?? [],
  }
}


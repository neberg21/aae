export type AgentDto = {
  agentId: string
  name: string
  department: string
  jobTitle: string
}

export type AgentDetail = AgentDto & {
  systemPrompt: string
}

export type AgentsPage = {
  items: AgentDto[]
  totalCount: number
  pageSize: number
  pageNumber: number
  totalPages: number
}

export type AgentSearchFilters = {
  name?: string
  department?: string
  jobTitle?: string
}

export type ChatRole = 'user' | 'assistant'

export type ChatMessage = {
  role: ChatRole
  content: string
}

export type ThreadSummary = {
  threadId: string
  createdAt: string
  updatedAt: string
  messageCount: number
}

export type ThreadsPage = {
  items: ThreadSummary[]
  totalCount: number
  pageSize: number
  pageNumber: number
  totalPages: number
}

export type ThreadMessage = {
  sender: string
  receiver: string
  content: string
  createdAt: string
}

export type ThreadDetail = {
  threadId: string
  messages: ThreadMessage[]
}


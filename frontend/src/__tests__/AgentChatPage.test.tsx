import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import AgentChatPage from '../modules/agents/AgentChatPage'
import { sendAgentChatMessage, getAgent } from '../modules/agents/api'

vi.mock('../modules/agents/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../modules/agents/api')>()
  return {
    ...actual,
    sendAgentChatMessage: vi.fn(),
    getAgent: vi.fn(),
  }
})

const sendAgentChatMessageMock = vi.mocked(sendAgentChatMessage)
const getAgentMock = vi.mocked(getAgent)

function renderPage(agentId: string = 'agent-1') {
  return render(
    <MemoryRouter initialEntries={[`/module/agents/chat/${agentId}`]}>
      <Routes>
        <Route path="/module/agents/chat/:agentId" element={<AgentChatPage />} />
        <Route path="/module/agents/list" element={<div>Agents List</div>} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('AgentChatPage', () => {
  afterEach(() => {
    cleanup()
  })

  beforeEach(() => {
    sendAgentChatMessageMock.mockReset()
    getAgentMock.mockReset()
    getAgentMock.mockResolvedValue({
      agentId: 'agent-1',
      name: 'TestAgent',
      department: 'QA',
      jobTitle: 'Tester',
      systemPrompt: 'You are a test agent.',
    })
  })

  it('loads agent name on mount', async () => {
    renderPage()

    await waitFor(() => {
      expect(getAgentMock).toHaveBeenCalledWith('agent-1')
    })

    expect(await screen.findByText(/Chat with TestAgent/i)).toBeInTheDocument()
  })

  it('sends a message to the agent and renders the reply', async () => {
    const user = userEvent.setup()
    sendAgentChatMessageMock.mockResolvedValue({
      threadId: 'thread-1',
      chatMessages: [
        {
          sender: 'user',
          receiver: 'agent-1',
          content: 'Hi TestAgent',
          createdAt: '2026-07-25T10:00:00Z',
        },
        {
          sender: 'agent-1',
          receiver: 'user',
          content: 'Hello, I am TestAgent. How can I help?',
          createdAt: '2026-07-25T10:00:01Z',
        },
      ],
    })

    renderPage()

    await waitFor(() => {
      expect(getAgentMock).toHaveBeenCalledTimes(1)
    })

    const textarea = await screen.findByRole('textbox', { name: /your message/i })
    await user.type(textarea, 'Hi TestAgent')
    await user.click(screen.getByRole('button', { name: /send/i }))

    await waitFor(() => {
      expect(sendAgentChatMessageMock).toHaveBeenCalledWith('agent-1', 'Hi TestAgent', undefined)
    })

    expect(await screen.findByText('Hi TestAgent')).toBeInTheDocument()
    expect(await screen.findByText('Hello, I am TestAgent. How can I help?')).toBeInTheDocument()
  })

  it('shows an error when the backend call fails', async () => {
    const user = userEvent.setup()
    sendAgentChatMessageMock.mockRejectedValue(new Error('Backend unavailable'))

    renderPage()

    await waitFor(() => {
      expect(getAgentMock).toHaveBeenCalledTimes(1)
    })

    const textarea = await screen.findByRole('textbox', { name: /your message/i })
    await user.type(textarea, 'Can you help me?')
    await user.click(screen.getByRole('button', { name: /send/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Backend unavailable')
  })

  it('clears the draft message after sending', async () => {
    const user = userEvent.setup()
    sendAgentChatMessageMock.mockResolvedValue({
      threadId: 'thread-2',
      chatMessages: [
        {
          sender: 'user',
          receiver: 'agent-1',
          content: 'Test message',
          createdAt: '2026-07-25T10:00:00Z',
        },
      ],
    })

    renderPage()

    await waitFor(() => {
      expect(getAgentMock).toHaveBeenCalledTimes(1)
    })

    const textarea = await screen.findByRole('textbox', { name: /your message/i })
    await user.type(textarea, 'Test message')
    await user.click(screen.getByRole('button', { name: /send/i }))

    await waitFor(() => {
      expect(sendAgentChatMessageMock).toHaveBeenCalledTimes(1)
    })

    expect(textarea).toHaveValue('')
  })

  it('disables send button while sending', async () => {
    const user = userEvent.setup()
    sendAgentChatMessageMock.mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 100, {
        threadId: 'thread-3',
        chatMessages: [],
      })),
    )

    renderPage()

    await waitFor(() => {
      expect(getAgentMock).toHaveBeenCalledTimes(1)
    })

    const textarea = await screen.findByRole('textbox', { name: /your message/i })
    const sendButton = screen.getByRole('button', { name: /send/i })

    await user.type(textarea, 'Test')
    await user.click(sendButton)

    expect(sendButton).toBeDisabled()
  })

  it('resets the conversation when "New conversation" button is clicked', async () => {
    const user = userEvent.setup()
    sendAgentChatMessageMock.mockResolvedValue({
      threadId: 'thread-4',
      chatMessages: [
        {
          sender: 'user',
          receiver: 'agent-1',
          content: 'First message',
          createdAt: '2026-07-25T10:00:00Z',
        },
      ],
    })

    renderPage()

    await waitFor(() => {
      expect(getAgentMock).toHaveBeenCalledTimes(1)
    })

    const textarea = await screen.findByRole('textbox', { name: /your message/i })
    await user.type(textarea, 'First message')
    await user.click(screen.getByRole('button', { name: /send/i }))

    await waitFor(() => {
      expect(screen.getByText('First message')).toBeInTheDocument()
    })

    const newConversationButton = screen.getByRole('button', { name: /new conversation/i })
    await user.click(newConversationButton)

    expect(screen.queryByText('First message')).not.toBeInTheDocument()
    expect(textarea).toHaveValue('')
  })

  it('passes threadId to subsequent messages', async () => {
    const user = userEvent.setup()
    sendAgentChatMessageMock.mockResolvedValue({
      threadId: 'thread-5',
      chatMessages: [
        {
          sender: 'user',
          receiver: 'agent-1',
          content: 'Message 1',
          createdAt: '2026-07-25T10:00:00Z',
        },
      ],
    })

    renderPage()

    await waitFor(() => {
      expect(getAgentMock).toHaveBeenCalledTimes(1)
    })

    const textarea = await screen.findByRole('textbox', { name: /your message/i })
    const sendButton = screen.getByRole('button', { name: /send/i })

    // Send first message
    await user.type(textarea, 'Message 1')
    await user.click(sendButton)

    await waitFor(() => {
      expect(sendAgentChatMessageMock).toHaveBeenCalledWith('agent-1', 'Message 1', undefined)
    })

    // Send second message
    await user.type(textarea, 'Message 2')
    await user.click(sendButton)

    // Second call should include threadId
    expect(sendAgentChatMessageMock).toHaveBeenLastCalledWith('agent-1', 'Message 2', 'thread-5')
  })

  it('displays agent name in messages and info section', async () => {
    sendAgentChatMessageMock.mockResolvedValue({
      threadId: 'thread-6',
      chatMessages: [
        {
          sender: 'user',
          receiver: 'agent-1',
          content: 'Hello',
          createdAt: '2026-07-25T10:00:00Z',
        },
        {
          sender: 'agent-1',
          receiver: 'user',
          content: 'Hi there',
          createdAt: '2026-07-25T10:00:01Z',
        },
      ],
    })

    renderPage()

    await waitFor(() => {
      expect(screen.getByText(/is available\./i)).toBeInTheDocument()
    })

    expect(screen.getByText('TestAgent is available.')).toBeInTheDocument()
  })

  it('shows loading state while loading agent', async () => {
    getAgentMock.mockImplementation(
      () => new Promise((resolve) =>
        setTimeout(
          () =>
            resolve({
              agentId: 'agent-1',
              name: 'TestAgent',
              department: 'QA',
              jobTitle: 'Tester',
              systemPrompt: 'You are a test agent.',
            }),
          100,
        ),
      ),
    )

    renderPage()

    expect(screen.getByText(/loading/i)).toBeInTheDocument()

    await waitFor(() => {
      expect(screen.queryByText(/loading/i)).not.toBeInTheDocument()
    })
  })

  it('handles error state when agent fails to load', async () => {
    getAgentMock.mockRejectedValue(new Error('Agent not found'))

    renderPage()

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Agent not found')
    })
  })
})


import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import LeoChatPage from '../modules/agents/LeoChatPage'
import { sendLeoMessage } from '../modules/agents/api'

vi.mock('../modules/agents/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../modules/agents/api')>()
  return {
    ...actual,
    sendLeoMessage: vi.fn(),
  }
})

const sendLeoMessageMock = vi.mocked(sendLeoMessage)

function renderPage() {
  return render(
    <MemoryRouter>
      <LeoChatPage />
    </MemoryRouter>,
  )
}

describe('LeoChatPage', () => {
  afterEach(() => {
    cleanup()
  })

  beforeEach(() => {
    sendLeoMessageMock.mockReset()
  })

  it('sends a message to Leo and renders the reply', async () => {
    const user = userEvent.setup()
    sendLeoMessageMock.mockResolvedValue({
      threadId: 'thread-1',
      reply: 'Hallo, ich bin Leo.',
      done: false,
      vision: null,
    })

    renderPage()

    await user.type(screen.getByRole('textbox', { name: /your message/i }), 'Hi Leo')
    await user.click(screen.getByRole('button', { name: /send/i }))

    expect(screen.getByText('Hi Leo')).toBeInTheDocument()

    await waitFor(() => {
      expect(sendLeoMessageMock).toHaveBeenCalledWith('Hi Leo', [], undefined)
    })

    expect(await screen.findByText('Hallo, ich bin Leo.')).toBeInTheDocument()
  })

  it('shows an error when the backend call fails', async () => {
    const user = userEvent.setup()
    sendLeoMessageMock.mockRejectedValue(new Error('Backend unavailable'))

    renderPage()

    await user.type(screen.getByRole('textbox', { name: /your message/i }), 'Kannst du helfen?')
    await user.click(screen.getByRole('button', { name: /send/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Backend unavailable')
  })

  it('marks chat as done when vision is returned', async () => {
    const user = userEvent.setup()
    sendLeoMessageMock.mockResolvedValue({
      threadId: 'thread-77',
      reply: 'Vision is complete.',
      done: true,
      vision: { threadId: 'thread-77' },
    })

    renderPage()

    await user.type(screen.getByRole('textbox', { name: /your message/i }), 'Define my vision')
    await user.click(screen.getByRole('button', { name: /send/i }))

    expect(await screen.findByText(/vision object is complete/i)).toBeInTheDocument()
    expect(screen.getByRole('textbox', { name: /your message/i })).toBeDisabled()
  })
})


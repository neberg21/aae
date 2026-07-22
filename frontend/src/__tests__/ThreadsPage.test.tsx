import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import ThreadsPage from '../modules/agents/ThreadsPage'
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
      <ThreadsPage />
    </MemoryRouter>,
  )
}

describe('ThreadsPage', () => {
  afterEach(() => {
    cleanup()
  })

  beforeEach(() => {
    sendLeoMessageMock.mockReset()
  })

  it('sends a message to Leo and renders the reply', async () => {
    const user = userEvent.setup()
    sendLeoMessageMock.mockResolvedValue('Hallo, ich bin Leo.')

    renderPage()

    await user.type(screen.getByRole('textbox', { name: /your message/i }), 'Hi Leo')
    await user.click(screen.getByRole('button', { name: /send/i }))

    expect(screen.getByText('Hi Leo')).toBeInTheDocument()

    await waitFor(() => {
      expect(sendLeoMessageMock).toHaveBeenCalledWith('Hi Leo', [], expect.any(String))
    })

    expect(await screen.findByText('Hallo, ich bin Leo.')).toBeInTheDocument()
  })

  it('shows an error when the webhook call fails', async () => {
    const user = userEvent.setup()
    sendLeoMessageMock.mockRejectedValue(new Error('Webhook unavailable'))

    renderPage()

    await user.type(screen.getByRole('textbox', { name: /your message/i }), 'Kannst du helfen?')
    await user.click(screen.getByRole('button', { name: /send/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Webhook unavailable')
  })
})


import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import ThreadsPage from '../modules/agents/ThreadsPage'
import { getThread, getThreads } from '../modules/agents/api'

vi.mock('../modules/agents/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../modules/agents/api')>()
  return {
    ...actual,
    getThreads: vi.fn(),
    getThread: vi.fn(),
  }
})

const getThreadsMock = vi.mocked(getThreads)
const getThreadMock = vi.mocked(getThread)

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
    getThreadsMock.mockReset()
    getThreadMock.mockReset()
  })

  it('loads threads and shows the selected thread messages', async () => {
    getThreadsMock.mockResolvedValue({
      items: [
        {
          threadId: 'thread-1',
          createdAt: '2026-07-22T14:01:17.8984086Z',
          updatedAt: '2026-07-22T14:01:17.8984086Z',
          messageCount: 1,
        },
        {
          threadId: 'thread-2',
          createdAt: '2026-07-22T14:05:17.8984086Z',
          updatedAt: '2026-07-22T14:06:17.8984086Z',
          messageCount: 2,
        },
      ],
      totalCount: 2,
      pageSize: 2,
      pageNumber: 1,
      totalPages: 1,
    })
    getThreadMock
      .mockResolvedValueOnce({
        threadId: 'thread-1',
        messages: [
          {
            sender: 'leo',
            receiver: 'helga',
            content: 'First thread message',
            createdAt: '2026-07-22T14:01:17.8984086Z',
          },
        ],
      })
      .mockResolvedValueOnce({
        threadId: 'thread-2',
        messages: [
          {
            sender: 'helga',
            receiver: 'leo',
            content: 'Second thread message',
            createdAt: '2026-07-22T14:06:17.8984086Z',
          },
        ],
      })

    const user = userEvent.setup()

    renderPage()

    expect(screen.getByRole('heading', { level: 1, name: /^threads$/i })).toBeInTheDocument()

    await waitFor(() => {
      expect(getThreadsMock).toHaveBeenCalledTimes(1)
    })

    await waitFor(() => {
      expect(getThreadMock).toHaveBeenCalledWith('thread-1')
    })

    expect(await screen.findByText('First thread message')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /thread-2/i }))

    await waitFor(() => {
      expect(getThreadMock).toHaveBeenCalledWith('thread-2')
    })

    expect(await screen.findByText('Second thread message')).toBeInTheDocument()
  })

  it('shows an empty state when there are no threads', async () => {
    getThreadsMock.mockResolvedValue({
      items: [],
      totalCount: 0,
      pageSize: 0,
      pageNumber: 1,
      totalPages: 0,
    })

    renderPage()

    expect(await screen.findByText(/no threads available/i)).toBeInTheDocument()
    expect(getThreadMock).not.toHaveBeenCalled()
  })
})


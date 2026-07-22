import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import AgentsListPage from '../modules/agents/AgentsListPage'
import { searchAgents } from '../modules/agents/api'

vi.mock('../modules/agents/api', () => ({
  searchAgents: vi.fn(),
}))

const searchAgentsMock = vi.mocked(searchAgents)

function renderPage() {
  return render(
    <MemoryRouter>
      <AgentsListPage />
    </MemoryRouter>,
  )
}

describe('AgentsListPage', () => {
  afterEach(() => {
    cleanup()
  })

  beforeEach(() => {
    searchAgentsMock.mockReset()
  })

  it('shows empty state and does not fetch when all filters are empty', async () => {
    const user = userEvent.setup()
    renderPage()

    await user.click(screen.getByRole('button', { name: /search/i }))

    expect(
      screen.getByText(/enter at least one filter/i),
    ).toBeInTheDocument()
    expect(searchAgentsMock).not.toHaveBeenCalled()
  })

  it('searches and renders result links to detail routes', async () => {
    const user = userEvent.setup()
    searchAgentsMock.mockResolvedValue({
      items: [
        {
          agentId: 'leo',
          name: 'Leo',
          department: 'Ops',
          jobTitle: 'Orchestrator',
        },
      ],
      totalCount: 1,
      pageSize: 1,
      pageNumber: 1,
      totalPages: 1,
    })

    renderPage()

    await user.type(screen.getByRole('textbox', { name: /^name$/i }), 'Leo')
    await user.click(screen.getByRole('button', { name: /search/i }))

    await waitFor(() => {
      expect(searchAgentsMock).toHaveBeenCalledWith({
        name: 'Leo',
        department: undefined,
        jobTitle: undefined,
      })
    })

    const link = await screen.findByRole('link', { name: /leo/i })
    expect(link).toHaveAttribute('href', '/module/agents/leo')
    expect(screen.getByText('Ops')).toBeInTheDocument()
    expect(screen.getByText('Orchestrator')).toBeInTheDocument()
  })
})

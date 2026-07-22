import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import AgentsListPage from '../modules/agents/AgentsListPage'
import { getAgents, searchAgents } from '../modules/agents/api'

vi.mock('../modules/agents/api', () => ({
  getAgents: vi.fn(),
  searchAgents: vi.fn(),
}))

const getAgentsMock = vi.mocked(getAgents)
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
    getAgentsMock.mockReset()
    searchAgentsMock.mockReset()
    getAgentsMock.mockResolvedValue({
      items: [
        {
          agentId: 'default',
          name: 'Default Agent',
          department: 'Ops',
          jobTitle: 'Responder',
        },
      ],
      totalCount: 1,
      pageSize: 1,
      pageNumber: 1,
      totalPages: 1,
    })
  })

  it('loads and shows all agents by default', async () => {
    renderPage()

    await waitFor(() => {
      expect(getAgentsMock).toHaveBeenCalledTimes(1)
    })

    expect(await screen.findByRole('link', { name: /default agent/i })).toHaveAttribute(
      'href',
      '/module/agents/default',
    )
  })

  it('loads all agents again when all filters are empty', async () => {
    const user = userEvent.setup()
    searchAgentsMock.mockResolvedValue({
      items: [],
      totalCount: 0,
      pageSize: 0,
      pageNumber: 1,
      totalPages: 0,
    })
    renderPage()

    await waitFor(() => {
      expect(getAgentsMock).toHaveBeenCalledTimes(1)
    })

    await user.type(screen.getByRole('textbox', { name: /^name$/i }), 'Leo')
    await waitFor(() => {
      expect(searchAgentsMock).toHaveBeenCalledTimes(1)
    })

    await user.clear(screen.getByRole('textbox', { name: /^name$/i }))

    await waitFor(() => {
      expect(getAgentsMock).toHaveBeenCalledTimes(2)
    })
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
    expect(searchAgentsMock).not.toHaveBeenCalled()

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

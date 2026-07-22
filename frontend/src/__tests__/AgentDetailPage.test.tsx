import { cleanup, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import AgentDetailPage from '../modules/agents/AgentDetailPage'
import { ApiError, getAgent } from '../modules/agents/api'

vi.mock('../modules/agents/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../modules/agents/api')>()
  return {
    ...actual,
    getAgent: vi.fn(),
  }
})

const getAgentMock = vi.mocked(getAgent)

function renderAt(id: string) {
  return render(
    <MemoryRouter initialEntries={[`/module/agents/${id}`]}>
      <Routes>
        <Route path="/module/agents/:id" element={<AgentDetailPage />} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('AgentDetailPage', () => {
  afterEach(() => {
    cleanup()
  })

  beforeEach(() => {
    getAgentMock.mockReset()
  })

  it('loads and shows agent fields', async () => {
    getAgentMock.mockResolvedValue({
      identityId: 'leo',
      name: 'Leo',
      department: 'Ops',
      jobTitle: 'Orchestrator',
      systemPrompt: 'You are Leo.',
    })

    renderAt('leo')

    await waitFor(() => {
      expect(getAgentMock).toHaveBeenCalledWith('leo')
    })

    expect(await screen.findByRole('heading', { name: 'Leo' })).toBeInTheDocument()
    expect(screen.getByText('Ops')).toBeInTheDocument()
    expect(screen.getByText('Orchestrator')).toBeInTheDocument()
    expect(screen.getByText('leo')).toBeInTheDocument()
    expect(screen.getByText('You are Leo.')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /back to agents/i })).toHaveAttribute(
      'href',
      '/module/agents',
    )
  })

  it('shows not-found on 404', async () => {
    getAgentMock.mockRejectedValue(new ApiError(404, 'Not Found'))

    renderAt('missing')

    expect(await screen.findByText(/agent not found/i)).toBeInTheDocument()
  })
})

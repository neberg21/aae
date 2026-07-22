import { cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '../App'

vi.mock('../modules/agents/api', () => ({
  searchAgents: vi.fn(),
  getAgent: vi.fn(),
  ApiError: class ApiError extends Error {
    status: number
    constructor(status: number, message: string) {
      super(message)
      this.name = 'ApiError'
      this.status = status
    }
  },
}))

describe('AppRoutes', () => {
  afterEach(() => {
    cleanup()
  })

  it('redirects / to /module/agents', async () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <AppRoutes />
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'Agents' })).toBeInTheDocument()
    expect(screen.getByText(/enter at least one filter/i)).toBeInTheDocument()
  })
})

import { cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it } from 'vitest'
import ThreadsPage from '../modules/agents/ThreadsPage'

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

  it('shows the restored threads placeholder', () => {
    renderPage()

    expect(screen.getByRole('heading', { name: /threads/i })).toBeInTheDocument()
    expect(screen.getByText(/to be implemented/i)).toBeInTheDocument()
  })
})


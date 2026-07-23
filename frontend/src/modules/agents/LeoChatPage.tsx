import { useRef, useState } from 'react'
import ModuleNavigation from './ModuleNavigation'
import { sendLeoMessage } from './api'
import type { ChatMessage } from './types'

type Status = 'idle' | 'sending' | 'error'

function createThreadId() {
  return `leo-thread-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`
}

export default function LeoChatPage() {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [draft, setDraft] = useState('')
  const [status, setStatus] = useState<Status>('idle')
  const [error, setError] = useState<string | null>(null)
  const threadIdRef = useRef(createThreadId())

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const message = draft.trim()
    if (!message || status === 'sending') {
      return
    }

    const history = [...messages]
    setMessages((current) => [...current, { role: 'user', content: message }])
    setDraft('')
    setError(null)
    setStatus('sending')

    try {
      const reply = await sendLeoMessage(message, history, threadIdRef.current)
      setMessages((current) => [...current, { role: 'assistant', content: reply }])
      setStatus('idle')
    } catch (err) {
      setStatus('error')
      setError(err instanceof Error ? err.message : 'Leo could not be reached')
    }
  }

  function handleReset() {
    threadIdRef.current = createThreadId()
    setMessages([])
    setDraft('')
    setError(null)
    setStatus('idle')
  }

  return (
    <main className="mx-auto my-8 w-full max-w-4xl px-4 text-left font-sans">
      <h1 className="text-center">Leo chat</h1>
      <ModuleNavigation />

      <section className="mb-6 rounded-xl border border-violet-200 bg-violet-50/70 p-4 text-sm text-violet-950 dark:border-violet-900/60 dark:bg-violet-950/20 dark:text-violet-100">
        <p className="font-semibold">Leo is ready.</p>
        <p className="mt-1 text-violet-900/80 dark:text-violet-100/80">
          Ask questions, describe a task, or continue a thread. The page keeps the local
          conversation history and routes your message through the default backend using a thread id.
        </p>
      </section>

      <section className="rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-700 dark:bg-gray-900">
        <div
          role="log"
          aria-live="polite"
          aria-label="Leo conversation"
          className="grid gap-4 border-b border-gray-200 p-4 dark:border-gray-700"
        >
          {messages.length === 0 && (
            <p className="rounded-lg border border-dashed border-gray-300 bg-gray-50 p-4 text-sm text-gray-600 dark:border-gray-700 dark:bg-gray-800/50 dark:text-gray-300">
              Start the conversation with Leo.
            </p>
          )}

          {messages.map((message, index) => (
            <article
              key={`${message.role}-${index}`}
              className={
                message.role === 'user'
                  ? 'ml-auto max-w-[85%] rounded-2xl rounded-br-md bg-violet-600 px-4 py-3 text-sm text-white'
                  : 'mr-auto max-w-[85%] rounded-2xl rounded-bl-md bg-gray-100 px-4 py-3 text-sm text-gray-900 dark:bg-gray-800 dark:text-gray-100'
              }
            >
              <p className="mb-1 text-xs font-semibold uppercase tracking-wide opacity-70">
                {message.role === 'user' ? 'You' : 'Leo'}
              </p>
              <p className="whitespace-pre-wrap">{message.content}</p>
            </article>
          ))}

          {status === 'sending' && (
            <p className="text-sm text-gray-600 dark:text-gray-300">Leo is thinking...</p>
          )}
        </div>

        <form onSubmit={handleSubmit} className="grid gap-3 p-4">
          <label className="grid gap-1 text-sm font-medium" htmlFor="leo-message">
            Your message
            <textarea
              id="leo-message"
              className="min-h-32 rounded-md border border-gray-300 bg-white px-3 py-2 text-base text-gray-900 shadow-sm outline-none transition disabled:cursor-not-allowed disabled:opacity-60 dark:border-gray-700 dark:bg-gray-950 dark:text-gray-100"
              value={draft}
              onChange={(event) => setDraft(event.target.value)}
              placeholder="Write to Leo..."
              disabled={status === 'sending'}
            />
          </label>

          {error && <p role="alert" className="text-sm text-red-600 dark:text-red-400">{error}</p>}

          <div className="flex flex-wrap gap-3">
            <button
              type="submit"
              disabled={status === 'sending' || draft.trim().length === 0}
              className="rounded-md bg-violet-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-violet-500 disabled:cursor-not-allowed disabled:opacity-60"
            >
              Send
            </button>
            <button
              type="button"
              onClick={handleReset}
              className="rounded-md border border-gray-300 px-4 py-2 text-sm font-medium text-gray-800 transition hover:bg-gray-100 dark:border-gray-700 dark:text-gray-100 dark:hover:bg-gray-800"
            >
              New conversation
            </button>
          </div>
        </form>
      </section>
    </main>
  )
}


import { useEffect, useState } from 'react'
import ModuleNavigation from './ModuleNavigation'
import { ApiError, getThread, getThreads } from './api'
import type { ThreadDetail, ThreadSummary } from './types'

type ListStatus = 'idle' | 'loading' | 'success' | 'error'
type DetailStatus = 'idle' | 'loading' | 'success' | 'notfound' | 'error'

function formatDateTime(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? value
    : new Intl.DateTimeFormat('de-DE', {
        dateStyle: 'medium',
        timeStyle: 'short',
      }).format(date)
}

export default function ThreadsPage() {
  const [threads, setThreads] = useState<ThreadSummary[]>([])
  const [listStatus, setListStatus] = useState<ListStatus>('idle')
  const [listError, setListError] = useState<string | null>(null)
  const [selectedThreadId, setSelectedThreadId] = useState<string | null>(null)
  const [detail, setDetail] = useState<ThreadDetail | null>(null)
  const [detailStatus, setDetailStatus] = useState<DetailStatus>('idle')
  const [detailError, setDetailError] = useState<string | null>(null)

  useEffect(() => {
    void loadThreads()
  }, [])

  useEffect(() => {
    if (!selectedThreadId) {
      setDetail(null)
      setDetailStatus('idle')
      setDetailError(null)
      return
    }

    let cancelled = false
    setDetailStatus('loading')
    setDetailError(null)

    getThread(selectedThreadId)
      .then((data) => {
        if (cancelled) return
        setDetail(data)
        setDetailStatus('success')
      })
      .catch((err: unknown) => {
        if (cancelled) return
        setDetail(null)
        if (err instanceof ApiError && err.status === 404) {
          setDetailStatus('notfound')
          return
        }

        setDetailStatus('error')
        setDetailError(err instanceof Error ? err.message : 'Failed to load thread')
      })

    return () => {
      cancelled = true
    }
  }, [selectedThreadId])

  async function loadThreads() {
    setListStatus('loading')
    setListError(null)

    try {
      const page = await getThreads()
      setThreads(page.items)
      setListStatus('success')
      setSelectedThreadId((current) => {
        if (page.items.length === 0) {
          return null
        }

        const hasCurrent = current && page.items.some((thread) => thread.threadId === current)
        return hasCurrent ? current : page.items[0]!.threadId
      })
    } catch (err) {
      setThreads([])
      setSelectedThreadId(null)
      setListStatus('error')
      setListError(err instanceof Error ? err.message : 'Failed to load threads')
    }
  }

  return (
    <main className="mx-auto my-8 w-full max-w-4xl px-4 text-left font-sans">
      <h1 className="text-center">Threads</h1>
      <ModuleNavigation />

      <section className="mb-6 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-gray-200 bg-white p-4 shadow-sm dark:border-gray-700 dark:bg-gray-900">
        <div>
          <h2 className="mb-1">Available threads</h2>
          <p className="text-sm text-gray-600 dark:text-gray-300">
            Review persisted conversations and inspect their messages.
          </p>
        </div>
        <button
          type="button"
          onClick={() => void loadThreads()}
          className="rounded-md border border-gray-300 px-4 py-2 text-sm font-medium text-gray-800 transition hover:bg-gray-100 dark:border-gray-700 dark:text-gray-100 dark:hover:bg-gray-800"
        >
          Refresh threads
        </button>
      </section>

      {listStatus === 'loading' && <p className="text-sm text-gray-600 dark:text-gray-300">Loading threads...</p>}
      {listStatus === 'error' && listError && <p role="alert" className="text-sm text-red-600 dark:text-red-400">{listError}</p>}

      {listStatus !== 'loading' && listStatus !== 'error' && threads.length === 0 && (
        <p className="text-sm text-gray-600 dark:text-gray-300">No threads available.</p>
      )}

      {threads.length > 0 && (
        <section className="grid gap-6 lg:grid-cols-[20rem_1fr]">
          <div className="rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-700 dark:bg-gray-900">
            <ul className="m-0 grid list-none gap-0 divide-y divide-gray-200 p-0 dark:divide-gray-700" aria-label="Thread list">
              {threads.map((thread) => {
                const isSelected = thread.threadId === selectedThreadId

                return (
                  <li key={thread.threadId}>
                    <button
                      type="button"
                      onClick={() => setSelectedThreadId(thread.threadId)}
                      className={
                        isSelected
                          ? 'grid w-full gap-1 bg-violet-50 px-4 py-3 text-left dark:bg-violet-950/30'
                          : 'grid w-full gap-1 px-4 py-3 text-left transition hover:bg-gray-50 dark:hover:bg-gray-800/60'
                      }
                    >
                      <span className="font-medium text-gray-900 dark:text-gray-100">{thread.threadId}</span>
                      <span className="text-xs text-gray-600 dark:text-gray-300">
                        {thread.messageCount} message{thread.messageCount === 1 ? '' : 's'}
                      </span>
                      <span className="text-xs text-gray-500 dark:text-gray-400">
                        Updated {formatDateTime(thread.updatedAt)}
                      </span>
                    </button>
                  </li>
                )
              })}
            </ul>
          </div>

          <div className="rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-700 dark:bg-gray-900">
            <div className="border-b border-gray-200 p-4 dark:border-gray-700">
              <h2 className="mb-1">Thread details</h2>
              {selectedThreadId && (
                <p className="text-sm text-gray-600 dark:text-gray-300">Selected thread: {selectedThreadId}</p>
              )}
            </div>

            <div className="grid gap-4 p-4">
              {detailStatus === 'loading' && (
                <p className="text-sm text-gray-600 dark:text-gray-300">Loading thread...</p>
              )}

              {detailStatus === 'notfound' && (
                <p className="text-sm text-gray-600 dark:text-gray-300">Thread not found.</p>
              )}

              {detailStatus === 'error' && detailError && (
                <p role="alert" className="text-sm text-red-600 dark:text-red-400">{detailError}</p>
              )}

              {detailStatus === 'success' && detail && (
                <>
                  <dl className="grid grid-cols-[8rem_1fr] gap-x-4 gap-y-2 rounded-lg border border-gray-200 p-4 text-sm dark:border-gray-700">
                    <dt className="font-semibold text-gray-700 dark:text-gray-200">Thread id</dt>
                    <dd className="text-gray-900 dark:text-gray-100">{detail.threadId}</dd>
                    <dt className="font-semibold text-gray-700 dark:text-gray-200">Messages</dt>
                    <dd className="text-gray-900 dark:text-gray-100">{detail.messages.length}</dd>
                  </dl>

                  {detail.messages.length === 0 ? (
                    <p className="text-sm text-gray-600 dark:text-gray-300">This thread has no messages.</p>
                  ) : (
                    <div role="log" aria-label="Thread messages" className="grid gap-3">
                      {detail.messages.map((message, index) => (
                        <article
                          key={`${message.createdAt}-${message.sender}-${index}`}
                          className="rounded-lg border border-gray-200 bg-gray-50 p-4 dark:border-gray-700 dark:bg-gray-800/40"
                        >
                          <div className="mb-2 flex flex-wrap items-center justify-between gap-2 text-xs text-gray-600 dark:text-gray-300">
                            <span>
                              <span className="font-semibold text-gray-800 dark:text-gray-100">{message.sender}</span>
                              {' → '}
                              <span className="font-semibold text-gray-800 dark:text-gray-100">{message.receiver ?? 'all'}</span>
                            </span>
                            <time dateTime={message.createdAt}>{formatDateTime(message.createdAt)}</time>
                          </div>
                          <p className="whitespace-pre-wrap text-sm text-gray-900 dark:text-gray-100">{message.content}</p>
                        </article>
                      ))}
                    </div>
                  )}
                </>
              )}

              {detailStatus === 'idle' && (
                <p className="text-sm text-gray-600 dark:text-gray-300">Select a thread to inspect its messages.</p>
              )}
            </div>
          </div>
        </section>
      )}
    </main>
  )
}


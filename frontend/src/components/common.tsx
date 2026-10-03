import { useEffect, useRef, useState, type ReactNode } from 'react'

export function CopyButton({ value, label = 'Копировать' }: { value: string; label?: string }) {
  const [copied, setCopied] = useState(false)

  useEffect(() => {
    if (!copied) return
    const timer = setTimeout(() => setCopied(false), 1500)
    return () => clearTimeout(timer)
  }, [copied])

  async function copy() {
    try {
      await navigator.clipboard.writeText(value)
      setCopied(true)
    } catch {
      window.prompt('Скопируйте вручную:', value)
    }
  }

  return (
    <button type="button" className="button small secondary" onClick={copy}>
      {copied ? 'Скопировано' : label}
    </button>
  )
}

export function ErrorText({ error }: { error: string | null }) {
  if (error == null) return null
  return (
    <p className="error" role="alert">
      {error}
    </p>
  )
}

export function BotBadge() {
  return (
    <span className="badge" title="Бот">
      бот
    </span>
  )
}

// Token secrets are shown once: the server keeps only their hash.
export function SecretReveal({
  title,
  token,
  onClose,
  children,
}: {
  title: string
  token: string
  onClose: () => void
  children?: ReactNode
}) {
  return (
    <div className="secret" role="status">
      <div className="secret-head">
        <strong>{title}</strong>
        <button type="button" className="button small ghost" onClick={onClose} aria-label="Скрыть">
          ✕
        </button>
      </div>
      <p className="muted">Сохраните его сейчас — больше он показан не будет.</p>
      <div className="secret-value">
        <code data-testid="secret-token">{token}</code>
        <CopyButton value={token} />
      </div>
      {children}
    </div>
  )
}

export function Modal({ title, onClose, children }: { title: string; onClose: () => void; children: ReactNode }) {
  const panel = useRef<HTMLDivElement>(null)

  useEffect(() => {
    panel.current?.querySelector<HTMLElement>('input, textarea, select')?.focus()
  }, [])

  useEffect(() => {
    function onKey(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  return (
    <div className="modal-backdrop" onMouseDown={(event) => event.target === event.currentTarget && onClose()}>
      <div className="modal" role="dialog" aria-modal="true" aria-label={title} ref={panel}>
        <div className="modal-head">
          <h2>{title}</h2>
          <button type="button" className="button small ghost" onClick={onClose} aria-label="Закрыть">
            ✕
          </button>
        </div>
        {children}
      </div>
    </div>
  )
}

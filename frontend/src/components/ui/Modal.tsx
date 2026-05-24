import { X } from 'lucide-react'
import type { PropsWithChildren } from 'react'

interface ModalProps extends PropsWithChildren {
  open: boolean
  onClose: () => void
  title: string
}

export function Modal({ open, onClose, title, children }: ModalProps) {
  if (!open) {
    return null
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-ink/60 px-4 py-8">
      <div className="panel-shell max-h-[90vh] w-full max-w-3xl overflow-y-auto p-6">
        <div className="mb-6 flex items-center justify-between gap-4">
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.28em] text-gold">Admin atelier</p>
            <h3 className="mt-2 text-2xl text-crimson">{title}</h3>
          </div>
          <button type="button" onClick={onClose} className="rounded-full border border-gold/35 p-2 text-ink/70 transition hover:border-crimson hover:text-crimson" aria-label="Close modal">
            <X className="h-5 w-5" />
          </button>
        </div>
        {children}
      </div>
    </div>
  )
}


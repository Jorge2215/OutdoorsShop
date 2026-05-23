import { AlertCircle, CheckCircle2, Info } from 'lucide-react'
import { cn } from '../../utils/cn'

interface AlertProps {
  title?: string
  message: string
  tone?: 'error' | 'success' | 'info'
}

const tones = {
  error: 'border-crimson/35 bg-crimson/8 text-crimson',
  success: 'border-jade/35 bg-jade/10 text-jade',
  info: 'border-gold/35 bg-gold/10 text-ink',
}

const icons = {
  error: AlertCircle,
  success: CheckCircle2,
  info: Info,
}

export function Alert({ title, message, tone = 'info' }: AlertProps) {
  const Icon = icons[tone]

  return (
    <div className={cn('rounded-2xl border px-4 py-3', tones[tone])} role="alert">
      <div className="flex items-start gap-3">
        <Icon className="mt-0.5 h-5 w-5 shrink-0" />
        <div>
          {title ? <p className="font-heading text-base">{title}</p> : null}
          <p className="text-sm">{message}</p>
        </div>
      </div>
    </div>
  )
}


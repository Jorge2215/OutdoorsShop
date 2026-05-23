import type { ReactNode } from 'react'
import { cn } from '../../utils/cn'

interface BadgeProps {
  children: ReactNode
  tone?: 'neutral' | 'success' | 'warning' | 'danger'
}

const tones = {
  neutral: 'border-gold/35 bg-gold/10 text-ink',
  success: 'border-jade/35 bg-jade/10 text-jade',
  warning: 'border-copper/35 bg-copper/10 text-copper',
  danger: 'border-crimson/35 bg-crimson/10 text-crimson',
}

export function Badge({ children, tone = 'neutral' }: BadgeProps) {
  return <span className={cn('inline-flex rounded-full border px-3 py-1 text-xs font-bold uppercase tracking-[0.18em]', tones[tone])}>{children}</span>
}


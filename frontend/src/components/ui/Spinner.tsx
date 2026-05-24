import { cn } from '../../utils/cn'

interface SpinnerProps {
  className?: string
}

export function Spinner({ className }: SpinnerProps) {
  return <span className={cn('inline-flex h-10 w-10 animate-spin rounded-full border-4 border-gold/25 border-t-gold', className)} aria-hidden="true" />
}


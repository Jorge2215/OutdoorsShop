import type { ButtonHTMLAttributes, PropsWithChildren } from 'react'
import { cn } from '../../utils/cn'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement>, PropsWithChildren {
  variant?: 'primary' | 'secondary' | 'ghost'
  size?: 'sm' | 'md' | 'lg'
  loading?: boolean
}

const variants = {
  primary: 'bg-crimson text-white shadow-lg shadow-crimson/20 hover:bg-crimson/90 focus-visible:ring-crimson/35',
  secondary: 'border border-gold/45 bg-white/70 text-ink hover:border-crimson hover:text-crimson focus-visible:ring-gold/35',
  ghost: 'bg-transparent text-ink hover:bg-gold/10 hover:text-crimson focus-visible:ring-gold/35',
}

const sizes = {
  sm: 'px-4 py-2 text-xs tracking-[0.24em]',
  md: 'px-5 py-3 text-sm tracking-[0.2em]',
  lg: 'px-6 py-3.5 text-sm tracking-[0.24em]',
}

export function Button({
  variant = 'primary',
  size = 'md',
  loading = false,
  className,
  children,
  disabled,
  type = 'button',
  ...props
}: ButtonProps) {
  return (
    <button
      type={type}
      className={cn(
        'inline-flex items-center justify-center rounded-full font-bold uppercase transition focus-visible:outline-none focus-visible:ring-2 disabled:cursor-not-allowed disabled:opacity-60',
        variants[variant],
        sizes[size],
        className,
      )}
      disabled={disabled || loading}
      {...props}
    >
      {loading ? <span className="mr-2 h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" /> : null}
      {children}
    </button>
  )
}


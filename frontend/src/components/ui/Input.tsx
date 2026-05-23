import type { InputHTMLAttributes, TextareaHTMLAttributes } from 'react'
import { cn } from '../../utils/cn'

interface FieldProps {
  label: string
  error?: string
}

type InputProps = FieldProps & InputHTMLAttributes<HTMLInputElement>
type TextareaProps = FieldProps & TextareaHTMLAttributes<HTMLTextAreaElement>

export function Input({ label, error, className, ...props }: InputProps) {
  return (
    <label className="block">
      <span className="field-label">{label}</span>
      <input className={cn('field-input', error && 'border-crimson focus:border-crimson focus:ring-crimson/20', className)} {...props} />
      {error ? <span className="mt-2 block text-sm text-crimson">{error}</span> : null}
    </label>
  )
}

export function Textarea({ label, error, className, ...props }: TextareaProps) {
  return (
    <label className="block">
      <span className="field-label">{label}</span>
      <textarea className={cn('field-input min-h-32 resize-y', error && 'border-crimson focus:border-crimson focus:ring-crimson/20', className)} {...props} />
      {error ? <span className="mt-2 block text-sm text-crimson">{error}</span> : null}
    </label>
  )
}


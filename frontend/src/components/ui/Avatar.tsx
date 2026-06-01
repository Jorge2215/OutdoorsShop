import { cn } from '../../utils/cn'

interface AvatarProps {
  src?: string | null
  alt: string
  initials: string
  className?: string
}

export function Avatar({ src, alt, initials, className }: AvatarProps) {
  return (
    <div
      className={cn(
        'flex items-center justify-center overflow-hidden rounded-full border border-gold/40 bg-ink text-gold shadow-lg shadow-ink/10',
        className,
      )}
    >
      {src ? (
        <img src={src} alt={alt} className="h-full w-full object-cover" />
      ) : (
        <span className="font-heading text-[1.7rem] uppercase tracking-[0.16em]">{initials}</span>
      )}
    </div>
  )
}

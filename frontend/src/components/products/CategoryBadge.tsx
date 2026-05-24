import type { Category } from '../../types/category'
import { getCategoryTone } from '../../utils/constants'
import { cn } from '../../utils/cn'

interface CategoryBadgeProps {
  category: Category
}

export function CategoryBadge({ category }: CategoryBadgeProps) {
  const tone = getCategoryTone(category.name)

  return (
    <span className={cn('inline-flex items-center rounded-full border px-3 py-1 text-xs font-bold uppercase tracking-[0.22em]', tone)}>
      {category.name}
    </span>
  )
}


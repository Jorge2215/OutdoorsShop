export const PLACEHOLDER_IMAGE = 'https://placehold.co/400x300/2D6A4F/C9A84C?text=OutdoorsShop'

export const categoryHighlights: Record<string, { title: string; description: string }> = {
  Camping: {
    title: 'Camp beneath warm skies',
    description: 'Shelter, sleep systems, and fire-side comfort for nights under open constellations.',
  },
  Trekking: {
    title: 'Follow distant ridgelines',
    description: 'Lightweight packs and dependable tools designed for long roads and changing weather.',
  },
  Cycling: {
    title: 'Ride with rhythm',
    description: 'Fast-moving essentials tuned for smooth roads, steep climbs, and daily range.',
  },
  Climbing: {
    title: 'Seek the vertical path',
    description: 'Precision gear for grip, confidence, and focus when the wall begins to rise.',
  },
}

export function getProductImage(imageUrl: string | null | undefined) {
  return imageUrl || PLACEHOLDER_IMAGE
}

export function getCategoryTone(categoryName: string) {
  switch (categoryName) {
    case 'Camping':
      return 'border-jade/35 bg-jade/10 text-jade'
    case 'Trekking':
      return 'border-copper/35 bg-copper/10 text-copper'
    case 'Cycling':
      return 'border-gold/35 bg-gold/10 text-gold'
    case 'Climbing':
      return 'border-crimson/35 bg-crimson/10 text-crimson'
    default:
      return 'border-gold/30 bg-mist text-ink'
  }
}


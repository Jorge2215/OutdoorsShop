export function formatCurrency(value: number) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(value)
}

export function formatDate(value: string) {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(value))
}

export function splitDisplayName(value: string) {
  const pieces = value.trim().split(/\s+/).filter(Boolean)
  if (pieces.length === 0) {
    return { firstName: '', lastName: '' }
  }
  if (pieces.length === 1) {
    return { firstName: pieces[0], lastName: '' }
  }
  return {
    firstName: pieces[0],
    lastName: pieces.slice(1).join(' '),
  }
}

export function getOrderTone(status: string): 'neutral' | 'success' | 'warning' | 'danger' {
  switch (status) {
    case 'Delivered':
      return 'success'
    case 'Cancelled':
      return 'danger'
    case 'Pending':
    case 'Processing':
    case 'Shipped':
      return 'warning'
    default:
      return 'neutral'
  }
}


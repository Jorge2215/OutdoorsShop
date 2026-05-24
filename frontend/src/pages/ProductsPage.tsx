import { Search } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { categoriesApi } from '../api/categories.api'
import { productsApi } from '../api/products.api'
import { ProductCard } from '../components/products/ProductCard'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { Card } from '../components/ui/Card'
import { SectionHeading } from '../components/ui/SectionHeading'
import { Spinner } from '../components/ui/Spinner'
import { useAsyncData } from '../hooks/useAsyncData'

const pageSize = 8

export default function ProductsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const initialCategory = searchParams.get('category') ? Number(searchParams.get('category')) : null
  const [selectedCategory, setSelectedCategory] = useState<number | null>(initialCategory)
  const [search, setSearch] = useState(searchParams.get('search') ?? '')
  const [page, setPage] = useState(1)

  useEffect(() => {
    const next = new URLSearchParams()
    if (selectedCategory) {
      next.set('category', String(selectedCategory))
    }
    if (search) {
      next.set('search', search)
    }
    setSearchParams(next, { replace: true })
  }, [search, selectedCategory, setSearchParams])

  const { data, loading, error } = useAsyncData(
    async () => {
      const [categories, products] = await Promise.all([
        categoriesApi.list(),
        productsApi.list({ categoryId: selectedCategory ?? undefined, search: search || undefined }),
      ])
      return {
        categories: categories.filter((category) => category.isActive),
        products: products.filter((product) => product.isActive),
      }
    },
    [selectedCategory, search],
  )

  const pagedProducts = useMemo(() => {
    const products = data?.products ?? []
    const start = (page - 1) * pageSize
    return products.slice(start, start + pageSize)
  }, [data?.products, page])

  const totalPages = useMemo(() => Math.max(1, Math.ceil((data?.products.length ?? 0) / pageSize)), [data?.products.length])

  return (
    <div className="container-shell py-14">
      <SectionHeading
        eyebrow="Catalog"
        title="Browse the expedition bazaar"
        description="Filter by route, search by name, and explore products arranged for clarity on every screen."
      />

      <div className="mt-10 grid gap-8 lg:grid-cols-[280px,1fr]">
        <aside className="space-y-6">
          <Card>
            <label className="field-label">Search</label>
            <div className="relative">
              <Search className="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-ink/40" />
              <input
                value={search}
                onChange={(event) => {
                  setSearch(event.target.value)
                  setPage(1)
                }}
                placeholder="Tent, lantern, rope..."
                className="field-input pl-11"
              />
            </div>
          </Card>
          <Card>
            <p className="field-label">Categories</p>
            <div className="flex flex-col gap-3">
              <Button
                variant={selectedCategory === null ? 'primary' : 'secondary'}
                className="justify-center"
                onClick={() => {
                  setSelectedCategory(null)
                  setPage(1)
                }}
              >
                All routes
              </Button>
              {data?.categories.map((category) => (
                <Button
                  key={category.id}
                  variant={selectedCategory === category.id ? 'primary' : 'secondary'}
                  className="justify-center"
                  onClick={() => {
                    setSelectedCategory(category.id)
                    setPage(1)
                  }}
                >
                  {category.name}
                </Button>
              ))}
            </div>
          </Card>
        </aside>

        <div>
          {loading ? (
            <div className="flex justify-center py-20"><Spinner /></div>
          ) : error ? (
            <Alert tone="error" title="Catalog unavailable" message={error} />
          ) : (data?.products.length ?? 0) === 0 ? (
            <Alert tone="info" title="No products found" message="Try another category or search term to reveal more trail-ready gear." />
          ) : (
            <>
              <div className="mb-6 flex items-center justify-between gap-3 rounded-[1.5rem] border border-gold/30 bg-white/80 px-5 py-4 shadow-sm">
                <div>
                  <p className="text-sm font-bold uppercase tracking-[0.24em] text-gold">Results</p>
                  <p className="text-lg text-ink">{data?.products.length} product{data?.products.length === 1 ? '' : 's'} found</p>
                </div>
                <p className="text-sm text-ink/60">Page {page} of {totalPages}</p>
              </div>
              <div className="grid gap-6 md:grid-cols-2 xl:grid-cols-3">
                {pagedProducts.map((product) => <ProductCard key={product.id} product={product} />)}
              </div>
              {totalPages > 1 && (
                <div className="mt-8 flex flex-wrap justify-center gap-3">
                  <Button variant="secondary" onClick={() => setPage((current) => Math.max(1, current - 1))} disabled={page === 1}>Previous</Button>
                  <Button variant="secondary" onClick={() => setPage((current) => Math.min(totalPages, current + 1))} disabled={page === totalPages}>Next</Button>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  )
}


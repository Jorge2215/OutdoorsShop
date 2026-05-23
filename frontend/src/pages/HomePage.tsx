import { Compass, Mountain, ShoppingBag, Sparkles } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { categoriesApi } from '../api/categories.api'
import { productsApi } from '../api/products.api'
import { ProductCard } from '../components/products/ProductCard'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { Card } from '../components/ui/Card'
import { SectionHeading } from '../components/ui/SectionHeading'
import { Spinner } from '../components/ui/Spinner'
import { useAsyncData } from '../hooks/useAsyncData'
import { categoryHighlights } from '../utils/constants'

export default function HomePage() {
  const navigate = useNavigate()
  const { data, loading, error } = useAsyncData(
    async () => {
      const [categories, products] = await Promise.all([categoriesApi.list(), productsApi.list()])
      return {
        categories: categories.filter((category) => category.isActive).slice(0, 4),
        products: products.filter((product) => product.isActive).slice(0, 4),
      }
    },
    [],
  )

  return (
    <div className="pb-16">
      <section className="hero-pattern relative overflow-hidden text-white">
        <div className="absolute inset-0 bamboo-grid opacity-20" />
        <div className="container-shell relative grid gap-12 py-20 lg:grid-cols-[1.25fr,0.95fr] lg:items-center lg:py-24">
          <div>
            <p className="text-sm font-bold uppercase tracking-[0.38em] text-gold">Magical & Oriental outdoors</p>
            <h1 className="mt-6 text-5xl text-white sm:text-6xl lg:text-7xl">Discover the Path</h1>
            <p className="mt-6 max-w-2xl text-lg text-white/80 sm:text-xl">
              Gear forged for the journey ahead—modern performance wrapped in the warmth of an ancient eastern bazaar.
            </p>
            <div className="mt-10 flex flex-wrap gap-4">
              <Button size="lg" onClick={() => navigate('/products')}>
                Explore the collection
              </Button>
              <Button size="lg" variant="secondary" onClick={() => navigate('/register')}>
                Begin your account
              </Button>
            </div>
          </div>
          <Card className="bg-white/10 p-8 text-white backdrop-blur-sm">
            <div className="grid gap-5 sm:grid-cols-2">
              {[
                { icon: Compass, title: 'Curated trails', text: 'Category-led discovery for every discipline.' },
                { icon: ShoppingBag, title: 'Cart harmony', text: 'Client-side cart that persists between visits.' },
                { icon: Mountain, title: 'Peak-ready stock', text: 'Inventory-aware shopping and admin insight.' },
                { icon: Sparkles, title: 'Protected journey', text: 'JWT auth with refresh flow and role-aware views.' },
              ].map((item) => (
                <div key={item.title} className="rounded-3xl border border-gold/30 bg-black/10 p-4">
                  <item.icon className="h-6 w-6 text-gold" />
                  <p className="mt-4 font-heading text-xl text-white">{item.title}</p>
                  <p className="mt-2 text-sm text-white/75">{item.text}</p>
                </div>
              ))}
            </div>
          </Card>
        </div>
      </section>

      <section className="container-shell mt-16">
        <SectionHeading
          eyebrow="Featured categories"
          title="Four routes, one enchanted marketplace"
          description="Browse the core disciplines the team defined—each with its own tone, palette, and adventurer mindset."
        />
        <div className="mt-10 grid gap-6 md:grid-cols-2 xl:grid-cols-4">
          {loading ? (
            <div className="col-span-full flex justify-center py-12"><Spinner /></div>
          ) : error ? (
            <div className="col-span-full"><Alert tone="error" title="Categories unavailable" message={error} /></div>
          ) : (
            data?.categories.map((category) => (
              <button key={category.id} type="button" className="text-left" onClick={() => navigate(`/products?category=${category.id}`)}>
                <Card className="h-full bg-white/90">
                  <p className="text-xs font-bold uppercase tracking-[0.32em] text-gold">{category.name}</p>
                  <h3 className="mt-4 text-2xl text-crimson">{categoryHighlights[category.name]?.title ?? category.name}</h3>
                  <p className="mt-3 text-sm text-ink/70">{categoryHighlights[category.name]?.description ?? 'Purposeful gear for your next horizon.'}</p>
                </Card>
              </button>
            ))
          )}
        </div>
      </section>

      <section className="container-shell mt-20">
        <SectionHeading
          eyebrow="Featured products"
          title="Lantern-lit essentials"
          description="Handpicked gear to turn the first click into a plan, a route, and a memory."
        />
        <div className="mt-10 grid gap-6 md:grid-cols-2 xl:grid-cols-4">
          {loading ? (
            <div className="col-span-full flex justify-center py-12"><Spinner /></div>
          ) : error ? (
            <div className="col-span-full"><Alert tone="error" title="Products unavailable" message={error} /></div>
          ) : (
            data?.products.map((product) => <ProductCard key={product.id} product={product} />)
          )}
        </div>
      </section>
    </div>
  )
}


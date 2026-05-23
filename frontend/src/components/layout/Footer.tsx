export function Footer() {
  return (
    <footer className="mt-16 border-t border-gold/25 bg-ink text-mist">
      <div className="container-shell grid gap-8 py-10 md:grid-cols-[1.4fr,1fr,1fr]">
        <div>
          <p className="font-heading text-2xl text-gold">OutdoorsShop</p>
          <p className="mt-3 max-w-md text-sm text-mist/80">
            An outdoor outfitter wrapped in warm lantern light—crafted for climbers, trekkers, campers, and riders who prefer wonder with their utility.
          </p>
        </div>
        <div>
          <p className="text-sm font-bold uppercase tracking-[0.25em] text-gold/90">Shop paths</p>
          <ul className="mt-3 space-y-2 text-sm text-mist/80">
            <li>Camping provisions</li>
            <li>Trekking essentials</li>
            <li>Cycling routes</li>
            <li>Climbing ascents</li>
          </ul>
        </div>
        <div>
          <p className="text-sm font-bold uppercase tracking-[0.25em] text-gold/90">Customer care</p>
          <ul className="mt-3 space-y-2 text-sm text-mist/80">
            <li>Secure checkout journey</li>
            <li>Role-aware storefront</li>
            <li>Live inventory insight</li>
            <li>Protected order history</li>
          </ul>
        </div>
      </div>
    </footer>
  )
}


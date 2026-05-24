import { useState } from 'react'
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom'
import { authApi } from '../api/auth.api'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { Card } from '../components/ui/Card'
import { Input } from '../components/ui/Input'
import { useAuthStore } from '../store/authStore'
import type { LoginRequest } from '../types/auth'

export default function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const [searchParams] = useSearchParams()
  const setTokenAndUser = useAuthStore((state) => state.setTokenAndUser)
  const [form, setForm] = useState<LoginRequest>({ email: '', password: '' })
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const nextFromState = typeof location.state?.from === 'string' ? location.state.from : null
  const nextFromQuery = searchParams.get('next')
  const redirectTarget = nextFromQuery || nextFromState || '/'

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setLoading(true)
    setError(null)

    try {
      const auth = await authApi.login(form)
      const user = await authApi.getMe(auth.accessToken)
      setTokenAndUser(auth.accessToken, user)
      navigate(user.role === 'Administrator' ? '/admin' : redirectTarget, { replace: true })
    } catch (caughtError) {
      setError(caughtError instanceof Error ? caughtError.message : 'Unable to sign in right now.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="container-shell grid gap-10 py-16 lg:grid-cols-[1.05fr,0.95fr] lg:items-center">
      <div>
        <p className="text-sm font-bold uppercase tracking-[0.35em] text-gold">Return to the bazaar</p>
        <h1 className="mt-5 text-5xl text-crimson">Sign in beneath the lanterns</h1>
        <p className="mt-5 max-w-xl text-lg text-ink/70">Access your profile, order history, and saved shopping cart through a secure JWT session with silent refresh.</p>
      </div>
      <Card>
        <form className="space-y-5" onSubmit={handleSubmit}>
          <Input label="Email" type="email" value={form.email} onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))} required />
          <Input label="Password" type="password" value={form.password} onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))} required />
          {error ? <Alert tone="error" title="Sign-in failed" message={error} /> : null}
          <Button className="w-full" type="submit" loading={loading}>Enter OutdoorsShop</Button>
          <p className="text-center text-sm text-ink/60">
            New here? <Link to="/register" className="font-bold text-crimson hover:text-ink">Create your account</Link>
          </p>
        </form>
      </Card>
    </div>
  )
}


import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { authApi } from '../api/auth.api'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { Card } from '../components/ui/Card'
import { Input } from '../components/ui/Input'
import { useAuthStore } from '../store/authStore'
import type { RegisterRequest } from '../types/auth'

const initialForm: RegisterRequest = {
  firstName: '',
  lastName: '',
  email: '',
  password: '',
  confirmPassword: '',
}

export default function RegisterPage() {
  const navigate = useNavigate()
  const setTokenAndUser = useAuthStore((state) => state.setTokenAndUser)
  const [form, setForm] = useState<RegisterRequest>(initialForm)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (form.password !== form.confirmPassword) {
      setError('Passwords must match.')
      return
    }

    setLoading(true)
    setError(null)

    try {
      const auth = await authApi.register(form)
      const user = await authApi.getMe(auth.accessToken)
      setTokenAndUser(auth.accessToken, user)
      navigate('/products', { replace: true })
    } catch (caughtError) {
      setError(caughtError instanceof Error ? caughtError.message : 'Unable to create your account right now.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="container-shell grid gap-10 py-16 lg:grid-cols-[1.05fr,0.95fr] lg:items-center">
      <div>
        <p className="text-sm font-bold uppercase tracking-[0.35em] text-gold">New traveler</p>
        <h1 className="mt-5 text-5xl text-crimson">Create your customer profile</h1>
        <p className="mt-5 max-w-xl text-lg text-ink/70">Register to manage orders, edit your profile, and carry your cart across refreshes with a secure refresh-cookie flow.</p>
      </div>
      <Card>
        <form className="grid gap-5 md:grid-cols-2" onSubmit={handleSubmit}>
          <Input label="First name" value={form.firstName} onChange={(event) => setForm((current) => ({ ...current, firstName: event.target.value }))} required />
          <Input label="Last name" value={form.lastName} onChange={(event) => setForm((current) => ({ ...current, lastName: event.target.value }))} required />
          <div className="md:col-span-2">
            <Input label="Email" type="email" value={form.email} onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))} required />
          </div>
          <Input label="Password" type="password" value={form.password} onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))} required minLength={8} />
          <Input label="Confirm password" type="password" value={form.confirmPassword} onChange={(event) => setForm((current) => ({ ...current, confirmPassword: event.target.value }))} required minLength={8} />
          {error ? <div className="md:col-span-2"><Alert tone="error" title="Registration failed" message={error} /></div> : null}
          <div className="md:col-span-2">
            <Button className="w-full" type="submit" loading={loading}>Create account</Button>
            <p className="mt-4 text-center text-sm text-ink/60">
              Already have an account? <Link to="/login" className="font-bold text-crimson hover:text-ink">Sign in instead</Link>
            </p>
          </div>
        </form>
      </Card>
    </div>
  )
}


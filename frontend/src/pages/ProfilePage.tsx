import { useEffect, useState } from 'react'
import { customersApi } from '../api/customers.api'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { Card } from '../components/ui/Card'
import { Input, Textarea } from '../components/ui/Input'
import { Spinner } from '../components/ui/Spinner'
import { useAuthStore } from '../store/authStore'

export default function ProfilePage() {
  const user = useAuthStore((state) => state.user)
  const [form, setForm] = useState({ firstName: '', lastName: '', phone: '', address: '' })
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  useEffect(() => {
    const loadProfile = async () => {
      if (!user?.customerId) {
        setLoading(false)
        return
      }

      try {
        const customer = await customersApi.getById(user.customerId)
        setForm({
          firstName: customer.firstName,
          lastName: customer.lastName,
          phone: customer.phone,
          address: customer.address,
        })
      } catch (caughtError) {
        setError(caughtError instanceof Error ? caughtError.message : 'Unable to load your profile.')
      } finally {
        setLoading(false)
      }
    }

    void loadProfile()
  }, [user?.customerId])

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!user?.customerId) {
      return
    }

    setSaving(true)
    setError(null)
    setSuccess(null)

    try {
      await customersApi.update(user.customerId, form)
      setSuccess('Your profile has been updated.')
    } catch (caughtError) {
      setError(caughtError instanceof Error ? caughtError.message : 'Unable to update your profile.')
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return <div className="container-shell flex justify-center py-20"><Spinner /></div>
  }

  return (
    <div className="container-shell py-14">
      <div className="grid gap-8 lg:grid-cols-[0.9fr,1.1fr]">
        <Card>
          <p className="text-sm font-bold uppercase tracking-[0.28em] text-gold">Customer profile</p>
          <h1 className="mt-4 text-4xl text-crimson">{user?.fullName || user?.email}</h1>
          <p className="mt-4 text-ink/70">Manage the contact details used during checkout and order delivery.</p>
          <div className="mt-8 rounded-3xl border border-gold/30 bg-white/75 p-5 text-sm text-ink/70">
            <p className="font-bold text-ink">Email</p>
            <p className="mt-2">{user?.email}</p>
          </div>
        </Card>
        <Card>
          <form className="grid gap-5 md:grid-cols-2" onSubmit={handleSubmit}>
            <Input label="First name" value={form.firstName} onChange={(event) => setForm((current) => ({ ...current, firstName: event.target.value }))} required />
            <Input label="Last name" value={form.lastName} onChange={(event) => setForm((current) => ({ ...current, lastName: event.target.value }))} required />
            <div className="md:col-span-2">
              <Input label="Phone" value={form.phone} onChange={(event) => setForm((current) => ({ ...current, phone: event.target.value }))} />
            </div>
            <div className="md:col-span-2">
              <Textarea label="Address" value={form.address} onChange={(event) => setForm((current) => ({ ...current, address: event.target.value }))} />
            </div>
            {error ? <div className="md:col-span-2"><Alert tone="error" title="Profile error" message={error} /></div> : null}
            {success ? <div className="md:col-span-2"><Alert tone="success" title="Profile saved" message={success} /></div> : null}
            <div className="md:col-span-2">
              <Button className="w-full" type="submit" loading={saving}>Save profile</Button>
            </div>
          </form>
        </Card>
      </div>
    </div>
  )
}


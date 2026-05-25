import { useEffect, useState } from 'react'
import { authApi } from '../api/auth.api'
import { customersApi } from '../api/customers.api'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { Card } from '../components/ui/Card'
import { Input, Textarea } from '../components/ui/Input'
import { Spinner } from '../components/ui/Spinner'
import { useAuthStore } from '../store/authStore'
import type { ChangePasswordRequest } from '../types/auth'

const initialProfileForm = { firstName: '', lastName: '', phone: '', address: '' }
const initialPasswordForm: ChangePasswordRequest = { currentPassword: '', newPassword: '', confirmNewPassword: '' }

type PasswordErrors = Partial<Record<keyof ChangePasswordRequest, string>>

export default function ProfilePage() {
  const user = useAuthStore((state) => state.user)
  const [profileForm, setProfileForm] = useState(initialProfileForm)
  const [passwordForm, setPasswordForm] = useState<ChangePasswordRequest>(initialPasswordForm)
  const [passwordErrors, setPasswordErrors] = useState<PasswordErrors>({})
  const [loading, setLoading] = useState(true)
  const [savingProfile, setSavingProfile] = useState(false)
  const [changingPassword, setChangingPassword] = useState(false)
  const [profileError, setProfileError] = useState<string | null>(null)
  const [profileSuccess, setProfileSuccess] = useState<string | null>(null)
  const [passwordError, setPasswordError] = useState<string | null>(null)
  const [passwordSuccess, setPasswordSuccess] = useState<string | null>(null)

  useEffect(() => {
    const loadProfile = async () => {
      if (!user?.customerId) {
        setLoading(false)
        return
      }

      try {
        const customer = await customersApi.getById(user.customerId)
        setProfileForm({
          firstName: customer.firstName,
          lastName: customer.lastName,
          phone: customer.phone,
          address: customer.address,
        })
      } catch (caughtError) {
        setProfileError(caughtError instanceof Error ? caughtError.message : 'Unable to load your profile.')
      } finally {
        setLoading(false)
      }
    }

    void loadProfile()
  }, [user?.customerId])

  const handleProfileSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!user?.customerId) {
      return
    }

    setSavingProfile(true)
    setProfileError(null)
    setProfileSuccess(null)

    try {
      await customersApi.update(user.customerId, profileForm)
      setProfileSuccess('Your profile has been updated.')
    } catch (caughtError) {
      setProfileError(caughtError instanceof Error ? caughtError.message : 'Unable to update your profile.')
    } finally {
      setSavingProfile(false)
    }
  }

  const handlePasswordChange = (field: keyof ChangePasswordRequest, value: string) => {
    setPasswordForm((current) => ({ ...current, [field]: value }))
    setPasswordErrors((current) => ({
      ...current,
      [field]: undefined,
      ...(field === 'newPassword' ? { confirmNewPassword: undefined } : {}),
    }))
    setPasswordError(null)
    setPasswordSuccess(null)
  }

  const validatePasswordForm = () => {
    const nextErrors: PasswordErrors = {}

    if (!passwordForm.currentPassword) {
      nextErrors.currentPassword = 'Enter your current password.'
    }

    if (!passwordForm.newPassword) {
      nextErrors.newPassword = 'Enter a new password.'
    } else if (passwordForm.newPassword.length < 8) {
      nextErrors.newPassword = 'Use at least 8 characters.'
    }

    if (!passwordForm.confirmNewPassword) {
      nextErrors.confirmNewPassword = 'Confirm your new password.'
    } else if (passwordForm.newPassword !== passwordForm.confirmNewPassword) {
      nextErrors.confirmNewPassword = 'New passwords must match.'
    }

    return nextErrors
  }

  const handlePasswordSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    const nextErrors = validatePasswordForm()
    if (Object.keys(nextErrors).length > 0) {
      setPasswordErrors(nextErrors)
      setPasswordError('Please correct the highlighted password fields.')
      setPasswordSuccess(null)
      return
    }

    setChangingPassword(true)
    setPasswordErrors({})
    setPasswordError(null)
    setPasswordSuccess(null)

    try {
      await authApi.changePassword(passwordForm)
      setPasswordForm(initialPasswordForm)
      setPasswordSuccess('Your password has been changed successfully.')
    } catch (caughtError) {
      setPasswordError(caughtError instanceof Error ? caughtError.message : 'Unable to change your password right now.')
    } finally {
      setChangingPassword(false)
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
          <p className="mt-4 text-ink/70">Manage the contact details used during checkout, order delivery, and account security.</p>
          <div className="mt-8 rounded-3xl border border-gold/30 bg-white/75 p-5 text-sm text-ink/70">
            <p className="font-bold text-ink">Email</p>
            <p className="mt-2">{user?.email}</p>
          </div>
        </Card>
        <Card>
          <form className="grid gap-5 md:grid-cols-2" onSubmit={handleProfileSubmit}>
            <Input label="First name" value={profileForm.firstName} onChange={(event) => setProfileForm((current) => ({ ...current, firstName: event.target.value }))} required />
            <Input label="Last name" value={profileForm.lastName} onChange={(event) => setProfileForm((current) => ({ ...current, lastName: event.target.value }))} required />
            <div className="md:col-span-2">
              <Input label="Phone" value={profileForm.phone} onChange={(event) => setProfileForm((current) => ({ ...current, phone: event.target.value }))} />
            </div>
            <div className="md:col-span-2">
              <Textarea label="Address" value={profileForm.address} onChange={(event) => setProfileForm((current) => ({ ...current, address: event.target.value }))} />
            </div>
            {profileError ? <div className="md:col-span-2"><Alert tone="error" title="Profile error" message={profileError} /></div> : null}
            {profileSuccess ? <div className="md:col-span-2"><Alert tone="success" title="Profile saved" message={profileSuccess} /></div> : null}
            <div className="md:col-span-2">
              <Button className="w-full" type="submit" loading={savingProfile}>Save profile</Button>
            </div>
          </form>

          <div className="mt-8 border-t border-gold/20 pt-8">
            <p className="text-sm font-bold uppercase tracking-[0.28em] text-gold">Change password</p>
            <p className="mt-3 text-ink/70">Update your password to keep your OutdoorsShop account secure.</p>

            <form className="mt-6 grid gap-5 md:grid-cols-2" onSubmit={handlePasswordSubmit} noValidate>
              <div className="md:col-span-2">
                <Input
                  label="Current password"
                  type="password"
                  value={passwordForm.currentPassword}
                  onChange={(event) => handlePasswordChange('currentPassword', event.target.value)}
                  error={passwordErrors.currentPassword}
                  autoComplete="current-password"
                />
              </div>
              <Input
                label="New password"
                type="password"
                value={passwordForm.newPassword}
                onChange={(event) => handlePasswordChange('newPassword', event.target.value)}
                error={passwordErrors.newPassword}
                autoComplete="new-password"
                minLength={8}
              />
              <Input
                label="Confirm new password"
                type="password"
                value={passwordForm.confirmNewPassword}
                onChange={(event) => handlePasswordChange('confirmNewPassword', event.target.value)}
                error={passwordErrors.confirmNewPassword}
                autoComplete="new-password"
                minLength={8}
              />
              {passwordError ? <div className="md:col-span-2"><Alert tone="error" title="Password update failed" message={passwordError} /></div> : null}
              {passwordSuccess ? <div className="md:col-span-2"><Alert tone="success" title="Password updated" message={passwordSuccess} /></div> : null}
              <div className="md:col-span-2">
                <Button className="w-full" type="submit" loading={changingPassword}>Change password</Button>
              </div>
            </form>
          </div>
        </Card>
      </div>
    </div>
  )
}


import { useEffect, useRef, useState } from 'react'
import { authApi } from '../api/auth.api'
import { customersApi } from '../api/customers.api'
import { Alert } from '../components/ui/Alert'
import { Avatar } from '../components/ui/Avatar'
import { Button } from '../components/ui/Button'
import { Card } from '../components/ui/Card'
import { Input, Textarea } from '../components/ui/Input'
import { Spinner } from '../components/ui/Spinner'
import { useAuthStore } from '../store/authStore'
import type { ChangePasswordRequest } from '../types/auth'
import type { Customer } from '../types/customer'

const initialProfileForm = { firstName: '', lastName: '', phone: '', address: '' }
const initialPasswordForm: ChangePasswordRequest = { currentPassword: '', newPassword: '', confirmNewPassword: '' }
const maxAvatarBytes = 2 * 1024 * 1024
const acceptedAvatarTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp']

type PasswordErrors = Partial<Record<keyof ChangePasswordRequest, string>>

function getDisplayName(firstName: string, lastName: string, fallback?: string | null) {
  const fullName = [firstName, lastName].map((value) => value.trim()).filter(Boolean).join(' ')
  return fullName || fallback || 'Your profile'
}

function getAvatarInitials(firstName: string, lastName: string, email?: string | null) {
  const initials = [firstName, lastName]
    .map((value) => value.trim().charAt(0))
    .filter(Boolean)
    .join('')
    .slice(0, 2)
    .toUpperCase()

  if (initials) {
    return initials
  }

  const emailInitials = (email ?? '')
    .split('@')[0]
    .replace(/[^a-zA-Z0-9]/g, '')
    .slice(0, 2)
    .toUpperCase()

  return emailInitials || 'OS'
}

export default function ProfilePage() {
  const user = useAuthStore((state) => state.user)
  const avatarInputRef = useRef<HTMLInputElement>(null)
  const [customer, setCustomer] = useState<Customer | null>(null)
  const [profileForm, setProfileForm] = useState(initialProfileForm)
  const [passwordForm, setPasswordForm] = useState<ChangePasswordRequest>(initialPasswordForm)
  const [passwordErrors, setPasswordErrors] = useState<PasswordErrors>({})
  const [loading, setLoading] = useState(true)
  const [savingProfile, setSavingProfile] = useState(false)
  const [changingPassword, setChangingPassword] = useState(false)
  const [avatarFile, setAvatarFile] = useState<File | null>(null)
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null)
  const [savingAvatar, setSavingAvatar] = useState(false)
  const [profileError, setProfileError] = useState<string | null>(null)
  const [profileSuccess, setProfileSuccess] = useState<string | null>(null)
  const [passwordError, setPasswordError] = useState<string | null>(null)
  const [passwordSuccess, setPasswordSuccess] = useState<string | null>(null)
  const [avatarError, setAvatarError] = useState<string | null>(null)
  const [avatarSuccess, setAvatarSuccess] = useState<string | null>(null)

  useEffect(() => {
    const loadProfile = async () => {
      if (!user?.customerId) {
        setLoading(false)
        return
      }

      try {
        const customer = await customersApi.getById(user.customerId)
        setCustomer(customer)
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

  useEffect(() => {
    return () => {
      if (avatarPreview) {
        URL.revokeObjectURL(avatarPreview)
      }
    }
  }, [avatarPreview])

  const clearAvatarSelection = () => {
    setAvatarFile(null)
    if (avatarPreview) {
      URL.revokeObjectURL(avatarPreview)
    }
    setAvatarPreview(null)
    if (avatarInputRef.current) {
      avatarInputRef.current.value = ''
    }
  }

  const handleProfileSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!user?.customerId) {
      return
    }

    setSavingProfile(true)
    setProfileError(null)
    setProfileSuccess(null)

    try {
      const updatedCustomer = await customersApi.update(user.customerId, profileForm)
      setCustomer(updatedCustomer)
      setProfileForm({
        firstName: updatedCustomer.firstName,
        lastName: updatedCustomer.lastName,
        phone: updatedCustomer.phone,
        address: updatedCustomer.address,
      })
      setProfileSuccess('Your profile has been updated.')
    } catch (caughtError) {
      setProfileError(caughtError instanceof Error ? caughtError.message : 'Unable to update your profile.')
    } finally {
      setSavingProfile(false)
    }
  }

  const handleAvatarFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const nextFile = event.target.files?.[0]
    setAvatarError(null)
    setAvatarSuccess(null)

    if (!nextFile) {
      clearAvatarSelection()
      return
    }

    if (!acceptedAvatarTypes.includes(nextFile.type)) {
      clearAvatarSelection()
      setAvatarError('Use JPG, PNG, GIF, or WEBP for your avatar.')
      return
    }

    if (nextFile.size > maxAvatarBytes) {
      clearAvatarSelection()
      setAvatarError('Avatar images must be 2 MB or smaller.')
      return
    }

    setAvatarFile(nextFile)
    if (avatarPreview) {
      URL.revokeObjectURL(avatarPreview)
    }
    setAvatarPreview(URL.createObjectURL(nextFile))
  }

  const handleAvatarUpload = async () => {
    if (!avatarFile || !user?.customerId) {
      return
    }

    setSavingAvatar(true)
    setAvatarError(null)
    setAvatarSuccess(null)

    try {
      const updatedCustomer = await customersApi.uploadAvatar(user.customerId, avatarFile)
      setCustomer(updatedCustomer)
      clearAvatarSelection()
      setAvatarSuccess('Avatar updated.')
    } catch (caughtError) {
      setAvatarError(caughtError instanceof Error ? caughtError.message : 'Unable to upload your avatar.')
    } finally {
      setSavingAvatar(false)
    }
  }

  const handleAvatarRemove = async () => {
    if (!user?.customerId || !customer?.avatarUrl) {
      return
    }

    setSavingAvatar(true)
    setAvatarError(null)
    setAvatarSuccess(null)

    try {
      const updatedCustomer = await customersApi.removeAvatar(user.customerId)
      setCustomer(updatedCustomer)
      clearAvatarSelection()
      setAvatarSuccess('Avatar removed. Initials are now shown.')
    } catch (caughtError) {
      setAvatarError(caughtError instanceof Error ? caughtError.message : 'Unable to remove your avatar.')
    } finally {
      setSavingAvatar(false)
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

  const displayName = getDisplayName(profileForm.firstName, profileForm.lastName, user?.fullName || user?.email)
  const avatarInitials = getAvatarInitials(profileForm.firstName, profileForm.lastName, user?.email)
  const avatarSrc = avatarPreview ?? customer?.avatarUrl

  return (
    <div className="container-shell py-14">
      <div className="grid gap-8 lg:grid-cols-[0.9fr,1.1fr]">
        <Card>
          <div className="flex flex-col gap-5 sm:flex-row sm:items-center">
            <Avatar
              src={avatarSrc}
              alt={`${displayName} avatar`}
              initials={avatarInitials}
              className="h-24 w-24 shrink-0"
            />
            <div>
              <p className="text-sm font-bold uppercase tracking-[0.28em] text-gold">Customer profile</p>
              <h1 className="mt-3 text-4xl text-crimson">{displayName}</h1>
              <p className="mt-3 text-sm uppercase tracking-[0.2em] text-ink/45">{avatarInitials} · account summary</p>
            </div>
          </div>
          <p className="mt-6 text-ink/70">Manage the contact details used during checkout, order delivery, and account security.</p>
          <div className="mt-6 rounded-3xl border border-gold/30 bg-white/75 p-5">
            <div className="flex flex-wrap items-center gap-3">
              <input
                ref={avatarInputRef}
                type="file"
                accept="image/jpeg,image/png,image/gif,image/webp"
                className="sr-only"
                onChange={handleAvatarFileChange}
              />
              <Button
                type="button"
                variant="secondary"
                onClick={() => avatarInputRef.current?.click()}
                disabled={savingAvatar}
              >
                {avatarFile ? 'Choose different photo' : customer?.avatarUrl ? 'Change photo' : 'Add photo'}
              </Button>
              {avatarFile ? (
                <>
                  <Button type="button" onClick={handleAvatarUpload} loading={savingAvatar}>
                    Save photo
                  </Button>
                  <Button type="button" variant="ghost" onClick={clearAvatarSelection} disabled={savingAvatar}>
                    Cancel
                  </Button>
                </>
              ) : customer?.avatarUrl ? (
                <Button type="button" variant="ghost" onClick={handleAvatarRemove} loading={savingAvatar}>
                  Remove photo
                </Button>
              ) : null}
            </div>
            <p className="mt-3 text-xs text-ink/45">
              JPG, PNG, GIF, or WEBP up to 2 MB. One avatar per customer, shown here with initials fallback.
            </p>
            {avatarFile ? <p className="mt-3 text-sm text-ink/70">Ready to upload: {avatarFile.name}</p> : null}
            {avatarError ? <div className="mt-4"><Alert tone="error" title="Avatar error" message={avatarError} /></div> : null}
            {avatarSuccess ? <div className="mt-4"><Alert tone="success" title="Avatar updated" message={avatarSuccess} /></div> : null}
          </div>
          <div className="mt-8 grid gap-4 text-sm text-ink/70 sm:grid-cols-2">
            <div className="rounded-3xl border border-gold/30 bg-white/75 p-5">
              <p className="font-bold text-ink">Email</p>
              <p className="mt-2 break-words">{user?.email}</p>
            </div>
            <div className="rounded-3xl border border-gold/30 bg-white/75 p-5">
              <p className="font-bold text-ink">Phone</p>
              <p className="mt-2">{profileForm.phone.trim() || 'Add a phone number'}</p>
            </div>
            <div className="rounded-3xl border border-gold/30 bg-white/75 p-5 sm:col-span-2">
              <p className="font-bold text-ink">Address</p>
              <p className="mt-2 whitespace-pre-line">{profileForm.address.trim() || 'Add a delivery address'}</p>
            </div>
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

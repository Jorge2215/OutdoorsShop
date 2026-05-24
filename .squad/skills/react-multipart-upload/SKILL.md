# Skill: React Multipart File Upload (with JWT auth)

**Domain:** React + TypeScript frontend API clients  
**Applies to:** Any authenticated `multipart/form-data` POST from a fetch-based API client

---

## Problem

Standard fetch wrappers that inject `Content-Type: application/json` will break multipart uploads. The browser must set `Content-Type: multipart/form-data; boundary=...` automatically — if you set it manually, the boundary is missing and the server rejects the request.

In this codebase, `fetchWithAuth` in `src/api/client.ts` calls `mergeHeaders()` which always injects `application/json` unless Content-Type is already present. Passing a `FormData` body still goes through this path, so a dedicated helper is required.

---

## Solution Pattern

### 1. Dedicated multipart helper in `client.ts`

```typescript
export async function fetchWithAuthMultipart<T>(
  path: string,
  body: FormData,
  retried = false,
): Promise<T> {
  const token = useAuthStore.getState().accessToken
  const headers = new Headers()           // ← empty: let browser add Content-Type + boundary
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const response = await fetch(buildApiUrl(path), {
    method: 'POST',
    headers,
    body,
    credentials: 'include',
  })

  if (response.status === 401 && !retried) {
    const refreshed = await useAuthStore.getState().refreshToken()
    if (refreshed) return fetchWithAuthMultipart<T>(path, body, true)
  }
  if (response.status === 401) {
    useAuthStore.getState().clearAuth()
    redirectToLogin()
  }
  return parseResponse<T>(response)
}
```

### 2. API method

```typescript
async uploadImage(productId: number, file: File): Promise<string> {
  const formData = new FormData()
  formData.append('file', file)
  const response = await fetchWithAuthMultipart<{ imageUrl: string } | string>(
    `/products/${productId}/image`,
    formData,
  )
  return typeof response === 'string' ? response : response.imageUrl
}
```

### 3. Component pattern

```tsx
function FileUploader({ onUploaded }: { onUploaded: (url: string) => void }) {
  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<string | null>(null)
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const f = e.target.files?.[0]
    if (!f) return
    if (!ACCEPTED_TYPES.includes(f.type)) { setError('Unsupported format'); return }
    if (f.size > MAX_BYTES) { setError('File too large'); return }
    setFile(f)
    setPreview(URL.createObjectURL(f))   // revoke on cleanup / new selection
  }

  async function handleUpload() {
    if (!file) return
    setUploading(true)
    try {
      const url = await someApi.uploadFile(file)
      onUploaded(url)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed')
    } finally {
      setUploading(false)
    }
  }

  return (/* file input + preview + upload button */)
}
```

---

## Key Rules

| Rule | Why |
|------|-----|
| Never set `Content-Type` for FormData | Browser must set it with boundary |
| Use `URL.createObjectURL` for preview | Instant, no upload needed |
| Revoke object URLs on unmount / new file | Prevents memory leaks |
| Validate MIME + size on client | Fast feedback before the network round-trip |
| Upload only on explicit button click | Avoids accidental uploads on file select |
| Sync URL back to parent form via `onUploaded` callback | Keeps parent form state authoritative |

---

## Files in this repo

- `frontend/src/api/client.ts` — `fetchWithAuthMultipart`
- `frontend/src/api/products.api.ts` — `productsApi.uploadImage`
- `frontend/src/components/products/ProductImageUpload.tsx` — reusable upload component

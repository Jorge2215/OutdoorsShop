import { ImagePlus, UploadCloud, X } from 'lucide-react'
import { useRef, useState } from 'react'
import { productsApi } from '../../api/products.api'
import { getProductImage } from '../../utils/constants'
import { Alert } from '../ui/Alert'
import { Button } from '../ui/Button'

const MAX_BYTES = 5 * 1024 * 1024
const ACCEPTED_TYPES = ['image/jpeg', 'image/png', 'image/gif', 'image/webp']
const ACCEPTED_LABELS = 'JPG, PNG, GIF, WEBP'

interface ProductImageUploadProps {
  productId: number
  currentImageUrl: string | null
  onUploaded: (newUrl: string) => void
}

export function ProductImageUpload({ productId, currentImageUrl, onUploaded }: ProductImageUploadProps) {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<string | null>(null)
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [successUrl, setSuccessUrl] = useState<string | null>(null)

  const displayUrl = successUrl ?? currentImageUrl

  function handleFileChange(event: React.ChangeEvent<HTMLInputElement>) {
    setError(null)
    setSuccessUrl(null)
    const file = event.target.files?.[0]
    if (!file) return

    if (!ACCEPTED_TYPES.includes(file.type)) {
      setError(`Unsupported format. Please upload ${ACCEPTED_LABELS}.`)
      return
    }
    if (file.size > MAX_BYTES) {
      setError('File is too large. Maximum size is 5 MB.')
      return
    }

    setSelectedFile(file)
    if (preview) URL.revokeObjectURL(preview)
    setPreview(URL.createObjectURL(file))
  }

  function handleClearSelection() {
    setSelectedFile(null)
    if (preview) URL.revokeObjectURL(preview)
    setPreview(null)
    setError(null)
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  async function handleUpload() {
    if (!selectedFile) return
    setUploading(true)
    setError(null)

    try {
      const newUrl = await productsApi.uploadImage(productId, selectedFile)
      setSuccessUrl(newUrl)
      onUploaded(newUrl)
      handleClearSelection()
    } catch (caughtError) {
      const message =
        caughtError instanceof Error
          ? caughtError.message
          : 'Upload failed. Please try again.'
      setError(message)
    } finally {
      setUploading(false)
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="overflow-hidden rounded-2xl border border-gold/30 bg-parchment">
        <img
          src={preview ?? getProductImage(displayUrl)}
          alt="Product image preview"
          className="h-52 w-full object-cover"
        />
      </div>

      {successUrl && !preview && (
        <p className="text-xs font-semibold text-jade">✓ Image updated successfully.</p>
      )}

      <div className="flex flex-col gap-2">
        <label
          htmlFor={`product-image-input-${productId}`}
          className="flex cursor-pointer items-center gap-3 rounded-xl border border-dashed border-gold/40 bg-white/60 px-4 py-3 text-sm text-ink/70 transition hover:border-gold hover:bg-white/80"
        >
          <ImagePlus className="h-5 w-5 shrink-0 text-gold" />
          <span className="flex-1 truncate">{selectedFile ? selectedFile.name : 'Choose an image…'}</span>
          {selectedFile && (
            <button
              type="button"
              onClick={(e) => { e.preventDefault(); handleClearSelection() }}
              className="shrink-0 rounded-full p-1 text-ink/50 transition hover:text-crimson"
              aria-label="Remove selected file"
            >
              <X className="h-4 w-4" />
            </button>
          )}
        </label>
        <input
          ref={fileInputRef}
          id={`product-image-input-${productId}`}
          type="file"
          accept="image/*"
          className="sr-only"
          onChange={handleFileChange}
        />
        <p className="text-xs text-ink/45">Accepted: {ACCEPTED_LABELS} · Max 5 MB</p>
      </div>

      {error && <Alert tone="error" title="Image error" message={error} />}

      {selectedFile && (
        <Button
          type="button"
          onClick={handleUpload}
          loading={uploading}
          className="self-start"
        >
          <UploadCloud className="mr-2 h-4 w-4" />
          {uploading ? 'Uploading…' : 'Upload image'}
        </Button>
      )}
    </div>
  )
}

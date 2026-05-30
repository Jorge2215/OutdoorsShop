import { Pencil, Plus, RotateCcw, Trash2 } from 'lucide-react'
import { useMemo, useRef, useState } from 'react'
import { categoriesApi } from '../../api/categories.api'
import { inventoryApi } from '../../api/inventory.api'
import { productsApi } from '../../api/products.api'
import { Alert } from '../../components/ui/Alert'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Input, Textarea } from '../../components/ui/Input'
import { Modal } from '../../components/ui/Modal'
import { Spinner } from '../../components/ui/Spinner'
import { ProductImageUpload } from '../../components/products/ProductImageUpload'
import { useAsyncData } from '../../hooks/useAsyncData'
import type { InventoryItem } from '../../types/inventory'
import type { Product, ProductUpsertRequest } from '../../types/product'
import { formatCurrency, formatDate } from '../../utils/format'

const emptyForm: ProductUpsertRequest = {
  name: '',
  description: '',
  price: 0,
  imageUrl: '',
  categoryId: 0,
  isActive: true,
}

export default function AdminProductsPage() {
  const { data, loading, error, reload } = useAsyncData(
    async () => {
      const [categories, products] = await Promise.all([categoriesApi.list(), productsApi.list({ includeInactive: true })])
      return { categories, products }
    },
    [],
  )
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<Product | null>(null)
  const [form, setForm] = useState<ProductUpsertRequest>(emptyForm)
  const [actionError, setActionError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [inventoryDetails, setInventoryDetails] = useState<InventoryItem | null>(null)
  const [inventoryLoading, setInventoryLoading] = useState(false)
  const [inventoryError, setInventoryError] = useState<string | null>(null)
  const [stockDraft, setStockDraft] = useState('')
  const inventoryRequestId = useRef(0)

  const activeCategories = useMemo(() => data?.categories.filter((category) => category.isActive) ?? [], [data?.categories])
  const stockEditingReady = editing !== null && inventoryDetails !== null && !inventoryLoading && !inventoryError

  const resetInventoryState = () => {
    inventoryRequestId.current += 1
    setInventoryDetails(null)
    setInventoryLoading(false)
    setInventoryError(null)
    setStockDraft('')
  }

  const closeModal = () => {
    setModalOpen(false)
    setEditing(null)
    setActionError(null)
    resetInventoryState()
  }

  const loadInventoryDetails = async (productId: number) => {
    const requestId = ++inventoryRequestId.current
    setInventoryLoading(true)
    setInventoryError(null)

    try {
      const inventory = await inventoryApi.getByProductId(productId)
      if (inventoryRequestId.current !== requestId) {
        return
      }

      setInventoryDetails(inventory)
      setStockDraft(String(inventory.quantityAvailable))
    } catch (caughtError) {
      if (inventoryRequestId.current !== requestId) {
        return
      }

      setInventoryDetails(null)
      setStockDraft('')
      setInventoryError(caughtError instanceof Error ? caughtError.message : 'Unable to load stock details.')
    } finally {
      if (inventoryRequestId.current === requestId) {
        setInventoryLoading(false)
      }
    }
  }

  const openCreate = () => {
    setEditing(null)
    setForm({ ...emptyForm, categoryId: activeCategories[0]?.id ?? 0 })
    setActionError(null)
    resetInventoryState()
    setModalOpen(true)
  }

  const openEdit = (product: Product) => {
    setEditing(product)
    setForm({
      name: product.name,
      description: product.description,
      price: product.price,
      imageUrl: product.imageUrl ?? '',
      categoryId: product.categoryId,
      isActive: product.isActive,
    })
    setActionError(null)
    setModalOpen(true)
    resetInventoryState()
    void loadInventoryDetails(product.id)
  }

  const handleSave = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const normalizedStock = stockDraft.trim()
    const stockValue = normalizedStock === '' ? Number.NaN : Number(normalizedStock)
    const shouldUpdateStock =
      Boolean(editing) &&
      inventoryDetails !== null &&
      !inventoryLoading &&
      normalizedStock !== '' &&
      stockValue !== inventoryDetails.quantityAvailable

    if (editing && inventoryDetails && (!Number.isInteger(stockValue) || stockValue < 0)) {
      setActionError('Stock available must be a whole number of 0 or more.')
      return
    }

    setSaving(true)
    setActionError(null)

    let productSaved = false

    try {
      let latestInventory: InventoryItem | null = null

      if (editing && shouldUpdateStock) {
        latestInventory = await inventoryApi.getByProductId(editing.id)
      }

      if (editing) {
        await productsApi.update(editing.id, form)
      } else {
        await productsApi.create(form)
      }
      productSaved = true

      if (editing && latestInventory) {
        const updatedInventory = await inventoryApi.update(editing.id, {
          quantityAvailable: stockValue,
          reorderThreshold: latestInventory.reorderThreshold,
        })

        setInventoryDetails(updatedInventory)
        setStockDraft(String(updatedInventory.quantityAvailable))
      }

      closeModal()
      reload()
    } catch (caughtError) {
      const message = caughtError instanceof Error ? caughtError.message : 'Unable to save the product.'
      if (productSaved && editing && shouldUpdateStock) {
        setActionError(`Product details were saved, but stock was not updated. ${message}`)
        reload()
      } else {
        setActionError(message)
      }
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (product: Product) => {
    if (!window.confirm(`Delete ${product.name}?`)) {
      return
    }

    setActionError(null)

    try {
      await productsApi.remove(product.id)
      reload()
    } catch (caughtError) {
      setActionError(caughtError instanceof Error ? caughtError.message : 'Unable to delete the product.')
    }
  }

  const handleReactivate = async (product: Product) => {
    setActionError(null)

    try {
      await productsApi.update(product.id, {
        name: product.name,
        description: product.description,
        price: product.price,
        imageUrl: product.imageUrl ?? '',
        categoryId: product.categoryId,
        isActive: true,
      })
      reload()
    } catch (caughtError) {
      setActionError(caughtError instanceof Error ? caughtError.message : 'Unable to reactivate the product.')
    }
  }

  return (
    <div className="container-shell py-14">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-4xl text-crimson">Admin products</h1>
          <p className="mt-3 text-ink/70">Manage product details, category assignment, and storefront visibility from one table.</p>
        </div>
        <Button onClick={openCreate}><Plus className="mr-2 h-4 w-4" /> Add product</Button>
      </div>

      <Card className="mt-8 overflow-hidden p-0">
        {loading ? (
          <div className="flex justify-center py-16"><Spinner /></div>
        ) : error ? (
          <div className="p-6"><Alert tone="error" title="Products unavailable" message={error} /></div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full text-left text-sm text-ink/70">
              <thead className="bg-ink text-xs uppercase tracking-[0.22em] text-gold">
                <tr>
                  <th className="px-6 py-4">Name</th>
                  <th className="px-6 py-4">Category</th>
                  <th className="px-6 py-4">Price</th>
                  <th className="px-6 py-4">Stock</th>
                  <th className="px-6 py-4">Active</th>
                  <th className="px-6 py-4 text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {data?.products.map((product) => (
                  <tr
                    key={product.id}
                    className={product.isActive ? 'border-t border-gold/20 bg-white/75' : 'border-t border-gold/20 bg-crimson/5 text-ink/60'}
                  >
                    <td className="px-6 py-4 font-semibold text-ink">{product.name}</td>
                    <td className="px-6 py-4">{product.category.name}</td>
                    <td className="px-6 py-4">{formatCurrency(product.price)}</td>
                    <td className="px-6 py-4">{product.quantityAvailable}</td>
                    <td className="px-6 py-4">
                      <Badge tone={product.isActive ? 'success' : 'danger'}>{product.isActive ? 'Active' : 'Inactive'}</Badge>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex justify-end gap-2">
                        <Button variant="secondary" size="sm" onClick={() => openEdit(product)}><Pencil className="mr-2 h-4 w-4" /> Edit</Button>
                        {product.isActive ? (
                          <Button variant="ghost" size="sm" onClick={() => handleDelete(product)}><Trash2 className="mr-2 h-4 w-4" /> Delete</Button>
                        ) : (
                          <Button variant="secondary" size="sm" onClick={() => handleReactivate(product)}><RotateCcw className="mr-2 h-4 w-4" /> Reactivate</Button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      <Modal open={modalOpen} onClose={closeModal} title={editing ? `Edit ${editing.name}` : 'Create product'}>
        <form className="grid gap-5 md:grid-cols-2" onSubmit={handleSave}>
          <Input label="Name" value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} required />
          <label className="block">
            <span className="field-label">Category</span>
            <select className="field-input" value={form.categoryId} onChange={(event) => setForm((current) => ({ ...current, categoryId: Number(event.target.value) }))} required>
              {activeCategories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
            </select>
          </label>
          <Input label="Price" type="number" min="0.01" step="0.01" value={String(form.price)} onChange={(event) => setForm((current) => ({ ...current, price: Number(event.target.value) }))} required />
          <Input label="Image URL" value={form.imageUrl ?? ''} onChange={(event) => setForm((current) => ({ ...current, imageUrl: event.target.value }))} />
          <div className="md:col-span-2">
            <Textarea label="Description" value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} />
          </div>
          {editing ? (
            <div className="md:col-span-2 rounded-[1.75rem] border border-gold/30 bg-white/60 p-5 shadow-gold">
              <div className="grid gap-4 md:grid-cols-[minmax(0,1fr)_minmax(0,0.95fr)] md:items-start">
                {stockEditingReady ? (
                  <Input
                    label="Stock available"
                    type="number"
                    min="0"
                    step="1"
                    value={stockDraft}
                    disabled={saving}
                    onChange={(event) => setStockDraft(event.target.value)}
                  />
                ) : (
                  <div className="rounded-2xl border border-dashed border-gold/35 bg-parchment/55 p-4">
                    <p className="field-label">Stock available</p>
                    <p className="mt-3 text-sm text-ink/70">
                      {inventoryLoading
                        ? 'Loading inventory details before stock can be edited.'
                        : 'Stock editing is unavailable until inventory loads successfully.'}
                    </p>
                  </div>
                )}
                <div className="rounded-2xl border border-gold/25 bg-parchment/80 p-4 text-sm text-ink/75">
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-ink/55">Inventory settings</p>
                  {inventoryLoading ? (
                    <div className="mt-3 flex items-center gap-3 text-ink/70">
                      <Spinner className="h-5 w-5 border-2" />
                      <span>Loading current stock and threshold…</span>
                    </div>
                  ) : stockEditingReady ? (
                    <div className="mt-3 space-y-2">
                      <p>
                        Reorder threshold stays at <span className="font-semibold text-ink">{inventoryDetails.reorderThreshold}</span>.
                      </p>
                      <p>Last inventory update {formatDate(inventoryDetails.lastUpdated)}.</p>
                      <p className="text-ink/60">Use Admin inventory if you need to change the reorder threshold itself.</p>
                    </div>
                  ) : (
                    <div className="mt-3 space-y-2 text-ink/65">
                      <p>Product details can still be saved, but stock will stay unchanged until inventory loads successfully.</p>
                      {inventoryError ? <p className="text-crimson/85">{inventoryError}</p> : null}
                    </div>
                  )}
                </div>
              </div>
              {inventoryError ? (
                <div className="mt-4">
                  <Alert
                    tone="error"
                    title="Stock details unavailable"
                    message={`${inventoryError} Product details can still be saved, but stock will not change until inventory loads successfully.`}
                  />
                </div>
              ) : inventoryLoading ? (
                <div className="mt-4">
                  <Alert
                    tone="info"
                    title="Stock editing temporarily locked"
                    message="You can keep editing product details and save them now, but stock stays read-only until the inventory request finishes."
                  />
                </div>
              ) : null}
            </div>
          ) : null}
          <label className="inline-flex items-center gap-3 text-sm font-semibold text-ink md:col-span-2">
            <input type="checkbox" checked={form.isActive ?? true} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} /> Active product
          </label>
          {editing && (
            <div className="md:col-span-2">
              <p className="field-label mb-3">Product image</p>
              <ProductImageUpload
                productId={editing.id}
                currentImageUrl={form.imageUrl ?? null}
                onUploaded={(newUrl) => setForm((current) => ({ ...current, imageUrl: newUrl }))}
              />
            </div>
          )}
          {actionError ? <div className="md:col-span-2"><Alert tone="error" title="Action failed" message={actionError} /></div> : null}
          <div className="flex justify-end gap-3 md:col-span-2">
            <Button variant="secondary" onClick={closeModal}>Cancel</Button>
            <Button type="submit" loading={saving}>{editing ? 'Save changes' : 'Create product'}</Button>
          </div>
        </form>
      </Modal>
    </div>
  )
}

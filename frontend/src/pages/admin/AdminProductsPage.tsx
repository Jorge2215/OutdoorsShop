import { Pencil, Plus, Trash2 } from 'lucide-react'
import { useMemo, useState } from 'react'
import { categoriesApi } from '../../api/categories.api'
import { productsApi } from '../../api/products.api'
import { Alert } from '../../components/ui/Alert'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Input, Textarea } from '../../components/ui/Input'
import { Modal } from '../../components/ui/Modal'
import { Spinner } from '../../components/ui/Spinner'
import { useAsyncData } from '../../hooks/useAsyncData'
import type { Product, ProductUpsertRequest } from '../../types/product'
import { formatCurrency } from '../../utils/format'

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
      const [categories, products] = await Promise.all([categoriesApi.list(), productsApi.list()])
      return { categories, products }
    },
    [],
  )
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<Product | null>(null)
  const [form, setForm] = useState<ProductUpsertRequest>(emptyForm)
  const [actionError, setActionError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const activeCategories = useMemo(() => data?.categories.filter((category) => category.isActive) ?? [], [data?.categories])

  const openCreate = () => {
    setEditing(null)
    setForm({ ...emptyForm, categoryId: activeCategories[0]?.id ?? 0 })
    setActionError(null)
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
  }

  const handleSave = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setSaving(true)
    setActionError(null)

    try {
      if (editing) {
        await productsApi.update(editing.id, form)
      } else {
        await productsApi.create(form)
      }
      setModalOpen(false)
      reload()
    } catch (caughtError) {
      setActionError(caughtError instanceof Error ? caughtError.message : 'Unable to save the product.')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (product: Product) => {
    if (!window.confirm(`Delete ${product.name}?`)) {
      return
    }

    try {
      await productsApi.remove(product.id)
      reload()
    } catch (caughtError) {
      setActionError(caughtError instanceof Error ? caughtError.message : 'Unable to delete the product.')
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
                  <tr key={product.id} className="border-t border-gold/20 bg-white/75">
                    <td className="px-6 py-4 font-semibold text-ink">{product.name}</td>
                    <td className="px-6 py-4">{product.category.name}</td>
                    <td className="px-6 py-4">{formatCurrency(product.price)}</td>
                    <td className="px-6 py-4">{product.quantityAvailable}</td>
                    <td className="px-6 py-4">{product.isActive ? 'Yes' : 'No'}</td>
                    <td className="px-6 py-4">
                      <div className="flex justify-end gap-2">
                        <Button variant="secondary" size="sm" onClick={() => openEdit(product)}><Pencil className="mr-2 h-4 w-4" /> Edit</Button>
                        <Button variant="ghost" size="sm" onClick={() => handleDelete(product)}><Trash2 className="mr-2 h-4 w-4" /> Delete</Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      <Modal open={modalOpen} onClose={() => setModalOpen(false)} title={editing ? `Edit ${editing.name}` : 'Create product'}>
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
          <label className="inline-flex items-center gap-3 text-sm font-semibold text-ink md:col-span-2">
            <input type="checkbox" checked={form.isActive ?? true} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} /> Active product
          </label>
          {actionError ? <div className="md:col-span-2"><Alert tone="error" title="Save failed" message={actionError} /></div> : null}
          <div className="flex justify-end gap-3 md:col-span-2">
            <Button variant="secondary" onClick={() => setModalOpen(false)}>Cancel</Button>
            <Button type="submit" loading={saving}>{editing ? 'Save changes' : 'Create product'}</Button>
          </div>
        </form>
      </Modal>
    </div>
  )
}


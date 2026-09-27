import { useEffect, useState } from 'react'
import { api, apiErrorMessage } from '../../services/api.js'

const emptyForm = {
  supplierId: '',
  materialId: '',
  availableQuantity: '',
  unitPrice: '',
  leadTimeDays: '',
}

export default function SupplierMaterialsPage() {
  const [supplierMaterials, setSupplierMaterials] = useState([])
  const [suppliers, setSuppliers] = useState([])
  const [materials, setMaterials] = useState([])

  const [form, setForm] = useState(emptyForm)
  const [editingId, setEditingId] = useState(null)

  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const loadData = async () => {
    try {
      setError('')

      const [
        supplierMaterialsResponse,
        suppliersResponse,
        materialsResponse,
      ] = await Promise.all([
        api.get('/SupplierMaterials'),
        api.get('/Suppliers'),
        api.get('/inventory/materials'),
      ])

      const supplierMaterialsData =
        supplierMaterialsResponse.data

      const suppliersData =
        suppliersResponse.data

      const materialsData =
        materialsResponse.data

      setSupplierMaterials(
        Array.isArray(supplierMaterialsData)
          ? supplierMaterialsData
          : supplierMaterialsData?.items ?? [],
      )

      setSuppliers(
        Array.isArray(suppliersData)
          ? suppliersData
          : suppliersData?.items ?? [],
      )

      setMaterials(
        Array.isArray(materialsData)
          ? materialsData
          : materialsData?.items ?? [],
      )
    } catch (err) {
      setError(apiErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadData()
  }, [])

  const updateField = (event) => {
    const { name, value } = event.target

    setForm((current) => ({
      ...current,
      [name]: value,
    }))
  }

  const resetForm = () => {
    setForm(emptyForm)
    setEditingId(null)
  }

  const handleSubmit = async (event) => {
    event.preventDefault()

    try {
      setSaving(true)
      setError('')

      const body = {
        supplierId: Number(form.supplierId),
        materialId: form.materialId,
        availableQuantity: Number(
          form.availableQuantity,
        ),
        unitPrice: Number(
          form.unitPrice,
        ),
        leadTimeDays: Number(
          form.leadTimeDays,
        ),
      }

      if (editingId) {
        await api.put(
          `/SupplierMaterials/${editingId}`,
          body,
        )
      } else {
        await api.post(
          '/SupplierMaterials',
          body,
        )
      }

      resetForm()
      await loadData()
    } catch (err) {
      setError(apiErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  const handleEdit = (item) => {
    setEditingId(item.id)

    setForm({
      supplierId:
        String(item.supplierId ?? ''),
      materialId:
        String(item.materialId ?? ''),
      availableQuantity:
        item.availableQuantity != null
          ? String(
              item.availableQuantity,
            )
          : '',
      unitPrice:
        item.unitPrice != null
          ? String(item.unitPrice)
          : '',
      leadTimeDays:
        item.leadTimeDays != null
          ? String(item.leadTimeDays)
          : '',
    })

    setError('')

    window.scrollTo({
      top: 0,
      behavior: 'smooth',
    })
  }

  const handleDeactivate = async (item) => {
    const confirmed = window.confirm(
      'Are you sure you want to remove this supplier material?',
    )

    if (!confirmed) {
      return
    }

    try {
      setError('')

      await api.delete(
        `/SupplierMaterials/${item.id}`,
      )

      await loadData()
    } catch (err) {
      setError(apiErrorMessage(err))
    }
  }

  const getSupplierName = (supplierId) => {
    const supplier = suppliers.find(
      (item) =>
        String(item.id) ===
        String(supplierId),
    )

    return (
      supplier?.name ??
      `Supplier #${supplierId}`
    )
  }

  const getMaterialName = (materialId) => {
    const material = materials.find(
      (item) =>
        String(item.id) ===
        String(materialId),
    )

    return (
      material?.name ??
      material?.materialName ??
      `Material #${materialId}`
    )
  }

  const formatCurrency = (value) => {
    if (value == null || value === '') {
      return '-'
    }

    const number = Number(value)

    if (Number.isNaN(number)) {
      return value
    }

    return number.toLocaleString(
      undefined,
      {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      },
    )
  }

  const activeLinks =
    supplierMaterials.filter(
      (item) =>
        item.isActive !== false,
    ).length

  if (loading) {
    return (
      <div className="page procurement-page">
        <p>
          Loading supplier materials...
        </p>
      </div>
    )
  }

  return (
    <div className="page procurement-page">
      <div className="page-header procurement-header">
        <div>
          <p className="eyebrow">
            PROCUREMENT
          </p>

          <h1>
            Supplier Materials
          </h1>

          <p>
            Manage materials supplied by each supplier,
            including price, availability and lead time.
          </p>
        </div>

        <div className="procurement-header-badge">
          Supplier material management
        </div>
      </div>

      {error && (
        <div className="alert alert-error">
          {error}
        </div>
      )}

      <div className="procurement-stats">
        <div className="procurement-stat-card">
          <div className="procurement-stat-icon blue">
            SM
          </div>

          <div>
            <span>
              Supplier Materials
            </span>

            <strong>
              {supplierMaterials.length}
            </strong>
          </div>
        </div>

        <div className="procurement-stat-card">
          <div className="procurement-stat-icon green">
            AC
          </div>

          <div>
            <span>
              Active Links
            </span>

            <strong>
              {activeLinks}
            </strong>
          </div>
        </div>

        <div className="procurement-stat-card">
          <div className="procurement-stat-icon purple">
            SU
          </div>

          <div>
            <span>
              Suppliers
            </span>

            <strong>
              {suppliers.length}
            </strong>
          </div>
        </div>

        <div className="procurement-stat-card">
          <div className="procurement-stat-icon yellow">
            MA
          </div>

          <div>
            <span>
              Materials
            </span>

            <strong>
              {materials.length}
            </strong>
          </div>
        </div>
      </div>

      <div className="card procurement-form-card">
        <div className="procurement-card-header">
          <div>
            <h2>
              {editingId
                ? 'Edit Supplier Material'
                : 'Add Supplier Material'}
            </h2>

            <p>
              Link a supplier with a material and
              define pricing and availability.
            </p>
          </div>
        </div>

        <div className="procurement-card-body">
          <form onSubmit={handleSubmit}>
            <div className="procurement-form-grid">
              <label className="field">
                <span>
                  Supplier
                </span>

                <select
                  name="supplierId"
                  value={form.supplierId}
                  onChange={updateField}
                  required
                >
                  <option value="">
                    Select supplier
                  </option>

                  {suppliers
                    .filter(
                      (supplier) =>
                        supplier.isActive !==
                        false,
                    )
                    .map((supplier) => (
                      <option
                        key={supplier.id}
                        value={supplier.id}
                      >
                        {supplier.name}
                      </option>
                    ))}
                </select>
              </label>

              <label className="field">
                <span>
                  Material
                </span>

                <select
                  name="materialId"
                  value={form.materialId}
                  onChange={updateField}
                  required
                >
                  <option value="">
                    Select material
                  </option>

                  {materials.map(
                    (material) => (
                      <option
                        key={material.id}
                        value={material.id}
                      >
                        {material.name ??
                          material.materialName}
                      </option>
                    ),
                  )}
                </select>
              </label>

              <label className="field">
                <span>
                  Available Quantity
                </span>

                <input
                  name="availableQuantity"
                  type="number"
                  min="0"
                  step="1"
                  value={
                    form.availableQuantity
                  }
                  onChange={updateField}
                  placeholder="Enter available quantity"
                  required
                />
              </label>

              <label className="field">
                <span>
                  Unit Price
                </span>

                <input
                  name="unitPrice"
                  type="number"
                  min="0.01"
                  step="0.01"
                  value={form.unitPrice}
                  onChange={updateField}
                  placeholder="Enter unit price"
                  required
                />
              </label>

              <label className="field">
                <span>
                  Lead Time (Days)
                </span>

                <input
                  name="leadTimeDays"
                  type="number"
                  min="0"
                  step="1"
                  value={form.leadTimeDays}
                  onChange={updateField}
                  placeholder="e.g. 7"
                  required
                />
              </label>
            </div>

            <div className="page-actions procurement-form-actions">
              <button
                className="button button-primary"
                type="submit"
                disabled={saving}
              >
                {saving
                  ? 'Saving...'
                  : editingId
                    ? 'Update Supplier Material'
                    : 'Add Supplier Material'}
              </button>

              {editingId && (
                <button
                  className="button button-ghost"
                  type="button"
                  onClick={resetForm}
                >
                  Cancel
                </button>
              )}
            </div>
          </form>
        </div>
      </div>

      <div className="card procurement-list-card">
        <div className="procurement-card-header">
          <div>
            <p className="eyebrow">
              SUPPLIER CATALOGUE
            </p>

            <h2>
              Supplier Materials
            </h2>

            <p>
              Review supplier pricing, stock availability
              and delivery lead times.
            </p>
          </div>
        </div>

        {supplierMaterials.length === 0 ? (
          <div className="procurement-empty">
            <div className="procurement-empty-icon">
              SM
            </div>

            <h3>
              No supplier materials found
            </h3>

            <p>
              Add a supplier material using the form
              above.
            </p>
          </div>
        ) : (
          <div className="supplier-material-grid">
            {supplierMaterials.map(
              (item) => {
                const isActive =
                  item.isActive !== false

                return (
                  <div
                    key={item.id}
                    className="supplier-material-card"
                  >
                    <div className="supplier-material-top">
                      <div className="supplier-material-icon">
                        SM
                      </div>

                      <div>
                        <h3>
                          {getMaterialName(
                            item.materialId,
                          )}
                        </h3>

                        <p>
                          {getSupplierName(
                            item.supplierId,
                          )}
                        </p>
                      </div>

                      <span
                        className={
                          isActive
                            ? 'supplier-status active'
                            : 'supplier-status inactive'
                        }
                      >
                        {isActive
                          ? 'Active'
                          : 'Inactive'}
                      </span>
                    </div>

                    <div className="supplier-material-details">
                      <div>
                        <span>
                          Available Quantity
                        </span>

                        <strong>
                          {item.availableQuantity ??
                            '-'}
                        </strong>
                      </div>

                      <div>
                        <span>
                          Unit Price
                        </span>

                        <strong>
                          {formatCurrency(
                            item.unitPrice,
                          )}
                        </strong>
                      </div>

                      <div>
                        <span>
                          Lead Time
                        </span>

                        <strong>
                          {item.leadTimeDays ??
                            '-'}{' '}
                          days
                        </strong>
                      </div>
                    </div>

                    <div className="supplier-actions">
                      <button
                        className="button button-ghost"
                        type="button"
                        onClick={() =>
                          handleEdit(item)
                        }
                        disabled={!isActive}
                      >
                        Edit
                      </button>

                      <button
                        className="button button-danger-ghost"
                        type="button"
                        onClick={() =>
                          handleDeactivate(
                            item,
                          )
                        }
                        disabled={!isActive}
                      >
                        Remove
                      </button>
                    </div>
                  </div>
                )
              },
            )}
          </div>
        )}
      </div>
    </div>
  )
}
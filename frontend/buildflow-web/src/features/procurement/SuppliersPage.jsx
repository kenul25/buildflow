import { useEffect, useState } from 'react'
import { api, apiErrorMessage } from '../../services/api.js'

const emptyForm = {
  name: '',
  contactPerson: '',
  phone: '',
  email: '',
  address: '',
}

export default function SuppliersPage() {
  const [suppliers, setSuppliers] = useState([])
  const [form, setForm] = useState(emptyForm)
  const [editingId, setEditingId] = useState(null)

  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const loadSuppliers = async () => {
    try {
      setError('')

      const response = await api.get('/Suppliers')

      const data = response.data

      setSuppliers(
        Array.isArray(data)
          ? data
          : data?.items ?? [],
      )
    } catch (err) {
      setError(apiErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadSuppliers()
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
        name: form.name.trim(),
        contactPerson: form.contactPerson.trim(),
        phone: form.phone.trim(),
        email: form.email.trim(),
        address: form.address.trim(),
      }

      if (editingId) {
        await api.put(
          `/Suppliers/${editingId}`,
          body,
        )
      } else {
        await api.post(
          '/Suppliers',
          body,
        )
      }

      resetForm()
      await loadSuppliers()
    } catch (err) {
      setError(apiErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  const handleEdit = (supplier) => {
    setEditingId(supplier.id)

    setForm({
      name: supplier.name ?? '',
      contactPerson:
        supplier.contactPerson ?? '',
      phone: supplier.phone ?? '',
      email: supplier.email ?? '',
      address: supplier.address ?? '',
    })

    setError('')

    window.scrollTo({
      top: 0,
      behavior: 'smooth',
    })
  }

  const handleDeactivate = async (supplier) => {
    const confirmed = window.confirm(
      `Are you sure you want to deactivate ${supplier.name}?`,
    )

    if (!confirmed) {
      return
    }

    try {
      setError('')

      await api.delete(
        `/Suppliers/${supplier.id}`,
      )

      await loadSuppliers()
    } catch (err) {
      setError(apiErrorMessage(err))
    }
  }

  const activeSuppliers = suppliers.filter(
    (supplier) =>
      supplier.isActive !== false,
  ).length

  const inactiveSuppliers = suppliers.filter(
    (supplier) =>
      supplier.isActive === false,
  ).length

  if (loading) {
    return (
      <div className="page procurement-page">
        <p>Loading suppliers...</p>
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
            Suppliers
          </h1>

          <p>
            Create and manage supplier information
            for construction procurement.
          </p>
        </div>

        <div className="procurement-header-badge">
          Supplier management
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
            SP
          </div>

          <div>
            <span>Total Suppliers</span>
            <strong>
              {suppliers.length}
            </strong>
          </div>
        </div>

        <div className="procurement-stat-card">
          <div className="procurement-stat-icon green">
            AC
          </div>

          <div>
            <span>Active</span>
            <strong>
              {activeSuppliers}
            </strong>
          </div>
        </div>

        <div className="procurement-stat-card">
          <div className="procurement-stat-icon yellow">
            IN
          </div>

          <div>
            <span>Inactive</span>
            <strong>
              {inactiveSuppliers}
            </strong>
          </div>
        </div>
      </div>

      <div className="card procurement-form-card">
        <div className="procurement-card-header">
          <div>
            <h2>
              {editingId
                ? 'Edit Supplier'
                : 'Create Supplier'}
            </h2>

            <p>
              {editingId
                ? 'Update supplier contact and business information.'
                : 'Enter supplier details to add a new procurement supplier.'}
            </p>
          </div>
        </div>

        <div className="procurement-card-body">
          <form onSubmit={handleSubmit}>
            <div className="procurement-form-grid">
              <label className="field">
                <span>
                  Supplier Name
                </span>

                <input
                  name="name"
                  value={form.name}
                  onChange={updateField}
                  placeholder="e.g. ABC Construction Supplies"
                  required
                />
              </label>

              <label className="field">
                <span>
                  Contact Person
                </span>

                <input
                  name="contactPerson"
                  value={form.contactPerson}
                  onChange={updateField}
                  placeholder="Enter contact person"
                  required
                />
              </label>

              <label className="field">
                <span>
                  Phone
                </span>

                <input
                  name="phone"
                  value={form.phone}
                  onChange={updateField}
                  placeholder="Enter phone number"
                  required
                />
              </label>

              <label className="field">
                <span>
                  Email
                </span>

                <input
                  name="email"
                  type="email"
                  value={form.email}
                  onChange={updateField}
                  placeholder="supplier@example.com"
                  required
                />
              </label>

              <label className="field procurement-full-field">
                <span>
                  Address
                </span>

                <textarea
                  name="address"
                  value={form.address}
                  onChange={updateField}
                  placeholder="Enter supplier address"
                  rows="3"
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
                    ? 'Update Supplier'
                    : 'Create Supplier'}
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
              SUPPLIER DIRECTORY
            </p>

            <h2>
              Suppliers
            </h2>

            <p>
              Review supplier information and manage
              supplier availability.
            </p>
          </div>
        </div>

        {suppliers.length === 0 ? (
          <div className="procurement-empty">
            <div className="procurement-empty-icon">
              SP
            </div>

            <h3>
              No suppliers found
            </h3>

            <p>
              Create your first supplier using the
              form above.
            </p>
          </div>
        ) : (
          <div className="supplier-grid">
            {suppliers.map((supplier) => {
              const isActive =
                supplier.isActive !== false

              return (
                <div
                  key={supplier.id}
                  className="supplier-card"
                >
                  <div className="supplier-card-top">
                    <div className="supplier-avatar">
                      {supplier.name
                        ? supplier.name
                            .charAt(0)
                            .toUpperCase()
                        : 'S'}
                    </div>

                    <div className="supplier-title">
                      <h3>
                        {supplier.name}
                      </h3>

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
                  </div>

                  <div className="supplier-details">
                    <div>
                      <span>
                        Contact Person
                      </span>

                      <strong>
                        {supplier.contactPerson ??
                          '-'}
                      </strong>
                    </div>

                    <div>
                      <span>
                        Phone
                      </span>

                      <strong>
                        {supplier.phone ?? '-'}
                      </strong>
                    </div>

                    <div>
                      <span>
                        Email
                      </span>

                      <strong>
                        {supplier.email ?? '-'}
                      </strong>
                    </div>

                    <div>
                      <span>
                        Address
                      </span>

                      <strong>
                        {supplier.address ?? '-'}
                      </strong>
                    </div>
                  </div>

                  <div className="supplier-actions">
                    <button
                      className="button button-ghost"
                      type="button"
                      onClick={() =>
                        handleEdit(supplier)
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
                          supplier,
                        )
                      }
                      disabled={!isActive}
                    >
                      Deactivate
                    </button>
                  </div>
                </div>
              )
            })}
          </div>
        )}
      </div>
    </div>
  )
}
import { useState } from 'react'
import { api, apiErrorMessage } from '../../services/api.js'

const getRankClass = (index) => {
  if (index === 0) {
    return 'comparison-rank rank-first'
  }

  if (index === 1) {
    return 'comparison-rank rank-second'
  }

  if (index === 2) {
    return 'comparison-rank rank-third'
  }

  return 'comparison-rank'
}

const formatPrice = (value) => {
  if (value === null || value === undefined || value === '') {
    return '-'
  }

  const number = Number(value)

  if (Number.isNaN(number)) {
    return value
  }

  return number.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })
}

export default function ProcurementComparisonPage() {
  const [materialName, setMaterialName] = useState('')
  const [quantity, setQuantity] = useState('')
  const [results, setResults] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const handleCompare = async (event) => {
    event.preventDefault()

    try {
      setLoading(true)
      setError('')
      setResults([])

      const response = await api.post('/Procurement/compare', {
        materialName: materialName.trim(),
        quantity: Number(quantity),
      })

      const data = response.data

      setResults(
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

  const clearComparison = () => {
    setMaterialName('')
    setQuantity('')
    setResults([])
    setError('')
  }

  const lowestPrice =
    results.length > 0
      ? Math.min(
          ...results
            .map((item) => Number(item.totalPrice))
            .filter((price) => !Number.isNaN(price)),
        )
      : null

  return (
    <div className="page comparison-page">

      {/* PAGE HEADER */}
      <div className="comparison-page-header">
        <div>
          <p className="eyebrow">PROCUREMENT</p>

          <h1>Procurement Comparison</h1>

          <p className="comparison-subtitle">
            Compare supplier quotations for a required material
            and quantity before making a procurement decision.
          </p>
        </div>

        {results.length > 0 && (
          <div className="comparison-summary-badge">
            <span>Quotations Found</span>
            <strong>{results.length}</strong>
          </div>
        )}
      </div>

      {/* ERROR */}
      {error && (
        <div className="comparison-alert">
          <strong>Comparison failed</strong>
          <span>{error}</span>
        </div>
      )}

      {/* SEARCH / COMPARISON FORM */}
      <div className="comparison-card">

        <div className="comparison-card-header">
          <div>
            <h2>Compare Supplier Quotations</h2>

            <p>
              Enter the material and required quantity to find
              and compare available supplier quotations.
            </p>
          </div>

          {(materialName || quantity || results.length > 0) && (
            <button
              className="button button-ghost"
              type="button"
              onClick={clearComparison}
            >
              Clear
            </button>
          )}
        </div>

        <form onSubmit={handleCompare}>

          <div className="comparison-form-grid">

            <label className="comparison-field">
              <span>Material Name</span>

              <input
                value={materialName}
                onChange={(event) =>
                  setMaterialName(event.target.value)
                }
                placeholder="e.g. Cement"
                required
              />

              <small>
                Enter the material required for the project.
              </small>
            </label>

            <label className="comparison-field">
              <span>Required Quantity</span>

              <input
                type="number"
                min="1"
                step="1"
                value={quantity}
                onChange={(event) =>
                  setQuantity(event.target.value)
                }
                placeholder="e.g. 100"
                required
              />

              <small>
                Enter the quantity you need to purchase.
              </small>
            </label>

          </div>

          <div className="comparison-form-actions">

            <button
              className="button button-primary comparison-button"
              type="submit"
              disabled={loading}
            >
              {loading ? (
                <>
                  <span className="comparison-button-spinner"></span>
                  Comparing...
                </>
              ) : (
                <>
                  <span className="comparison-button-icon">
                    ⇄
                  </span>
                  Compare Quotations
                </>
              )}
            </button>

          </div>

        </form>
      </div>

      {/* RESULTS */}
      {results.length > 0 && (
        <div className="comparison-card comparison-results-card">

          <div className="comparison-card-header">
            <div>
              <p className="eyebrow">RESULTS</p>

              <h2>Quotation Comparison</h2>

              <p>
                Supplier quotations matching{' '}
                <strong>{materialName}</strong> for{' '}
                <strong>{quantity}</strong> units.
              </p>
            </div>
          </div>

          {/* RESULT SUMMARY */}
          <div className="comparison-metrics">

            <div className="comparison-metric">
              <span>Total Quotations</span>
              <strong>{results.length}</strong>
            </div>

            <div className="comparison-metric">
              <span>Required Quantity</span>
              <strong>{quantity}</strong>
            </div>

            <div className="comparison-metric">
              <span>Lowest Total Price</span>
              <strong>
                {lowestPrice !== null
                  ? formatPrice(lowestPrice)
                  : '-'}
              </strong>
            </div>

          </div>

          {/* SUPPLIER RESULTS */}
          <div className="comparison-results">

            {results.map((item, index) => {
              const supplierName =
                item.supplierName ??
                item.supplier?.name ??
                'Supplier'

              const itemTotalPrice =
                item.totalPrice !== null &&
                item.totalPrice !== undefined &&
                item.totalPrice !== ''
                  ? Number(item.totalPrice)
                  : null

              const isLowest =
                lowestPrice !== null &&
                itemTotalPrice !== null &&
                itemTotalPrice === lowestPrice

              return (
                <div
                  key={item.id ?? index}
                  className={`comparison-result ${
                    isLowest
                      ? 'comparison-result-best'
                      : ''
                  }`}
                >

                  {/* RANK */}
                  <div className={getRankClass(index)}>
                    {index + 1}
                  </div>

                  {/* SUPPLIER INFO */}
                  <div className="comparison-supplier">

                    <div className="comparison-supplier-icon">
                      {supplierName
                        .charAt(0)
                        .toUpperCase()}
                    </div>

                    <div>
                      <div className="comparison-supplier-name">
                        {supplierName}
                      </div>

                      <div className="comparison-supplier-material">
                        {item.materialName ??
                          materialName}
                      </div>
                    </div>

                  </div>

                  {/* QUANTITY */}
                  <div className="comparison-result-detail">
                    <span>Quantity</span>

                    <strong>
                      {item.quantity ?? quantity}
                    </strong>
                  </div>

                  {/* UNIT PRICE */}
                  <div className="comparison-result-detail">
                    <span>Unit Price</span>

                    <strong>
                      {formatPrice(item.unitPrice)}
                    </strong>
                  </div>

                  {/* TOTAL PRICE */}
                  <div className="comparison-result-detail">
                    <span>Total Price</span>

                    <strong className="comparison-total-price">
                      {formatPrice(item.totalPrice)}
                    </strong>

                    {isLowest && (
                      <small className="lowest-price-label">
                        Lowest price
                      </small>
                    )}
                  </div>

                  {/* DELIVERY DATE */}
                  <div className="comparison-result-detail">
                    <span>Delivery Date</span>

                    <strong>
                      {item.deliveryDate ?? '-'}
                    </strong>
                  </div>

                </div>
              )
            })}

          </div>

        </div>
      )}

      {/* EMPTY STATE */}
      {!loading &&
        results.length === 0 &&
        !error && (
          <div className="comparison-empty">

            <div className="comparison-empty-icon">
              ⇄
            </div>

            <h3>Ready to compare quotations</h3>

            <p>
              Enter a material name and required quantity
              above, then click <strong>Compare Quotations</strong>.
            </p>

          </div>
        )}

      {/* LOADING STATE */}
      {loading && (
        <div className="comparison-loading">

          <div className="comparison-spinner"></div>

          <h3>Comparing supplier quotations...</h3>

          <p>
            Please wait while BuildFlow checks the available
            procurement quotations.
          </p>

        </div>
      )}

    </div>
  )
}
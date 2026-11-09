export function CourierDashboardPage() {
  return (
    <section className="welcome-card" aria-labelledby="page-title">
      <p className="eyebrow">Courier workspace</p>
      <h1 id="page-title">Courier dashboard</h1>
      <p className="intro">
        This is the starting point for your courier parcel-management UI.
        Assigned deliveries, status updates, proof uploads, and failure reports
        will be added here and connected to the Dispatch and Parcels APIs.
      </p>
      <div className="next-steps">
        <h2>Planned dashboard features</h2>
        <ul>
          <li>View deliveries assigned to the signed-in courier</li>
          <li>Update parcel status and review its status history</li>
          <li>Upload proof of delivery or report a failed delivery</li>
        </ul>
      </div>
    </section>
  )
}

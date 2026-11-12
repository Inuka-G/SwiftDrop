import { useEffect, useState } from 'react'

/**
 * A temporary browser URL for a Blob or File (for <img src>), revoked when the blob
 * changes or the component unmounts. The URL is an external resource, so it is created
 * in an effect: creating it during render would leak one URL per StrictMode re-render.
 */
export function useObjectUrl(blob: Blob | null | undefined): string | null {
  const [url, setUrl] = useState<string | null>(null)

  useEffect(() => {
    if (!blob) {
      setUrl(null) // eslint-disable-line react/set-state-in-effect -- mirrors the external resource
      return
    }
    const objectUrl = URL.createObjectURL(blob)
    setUrl(objectUrl)
    return () => URL.revokeObjectURL(objectUrl)
  }, [blob])

  return blob ? url : null
}

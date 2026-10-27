import { useParams } from 'react-router-dom'

function UserDetailPage() {
    const { id } = useParams()
    return <h1>User </h1>
}

export default UserDetailPage
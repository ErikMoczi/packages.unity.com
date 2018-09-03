using System;
using UnityEngine;

namespace UnityEditor.Experimental.U2D.Common
{
    internal interface IImagePackNodeVisitor
    {
        void Visit(ImagePackNode node);
    }

    class CollectPackNodePositionVisitor : IImagePackNodeVisitor
    {
        public CollectPackNodePositionVisitor()
        {
            positions = new Vector2Int[0];
        }

        public void Visit(ImagePackNode node)
        {
            if (node.imageId != -1)
            {
                if (positions.Length < node.imageId + 1)
                {
                    var p = positions;
                    Array.Resize(ref p, node.imageId + 1);
                    positions = p;
                }

                positions[node.imageId].x = node.rect.x;
                positions[node.imageId].y = node.rect.y;
            }
        }

        public Vector2Int[] positions { get; private set; }
    }

    internal class ImagePackNode
    {
        public ImagePackNode left;
        public ImagePackNode right;
        public RectInt rect;
        public int imageId = -1;

        public void AcceptVisitor(IImagePackNodeVisitor visitor)
        {
            visitor.Visit(this);
            if (left != null)
                left.AcceptVisitor(visitor);
            if (right != null)
                right.AcceptVisitor(visitor);
        }

        public void AdjustSize(int newWidth, int newHeight)
        {
            var dw = Mathf.Max(0, newWidth - rect.width);
            var dh = Mathf.Max(0, newHeight - rect.height);

            if (left != null)
            {
                int adjustXLeft = 0, adjustYLeft = 0;
                int adjustXRight = 0, adjustYRight = 0;
                left.AdjustSizeNotRoot(rect.width, rect.height, dw, dh, out adjustXLeft, out adjustYLeft);
                right.AdjustSizeNotRoot(rect.width, rect.height, dw - adjustXLeft , dh - adjustYLeft, out adjustXRight, out adjustYRight);
                // Assert.AreEqual(dw, Mathf.Max(adjustXLeft, adjustXRight));
                // Assert.AreEqual(dh, Mathf.Max(adjustYLeft, adjustYRight));
            }

            rect.width += dw;
            rect.height += dh;
        }

        public bool Insert(ImagePacker.ImagePackRect insert, int padding)
        {
            int insertWidth = insert.rect.width + padding * 2;
            int insertHeight = insert.rect.height + padding * 2;
            if (insertWidth > rect.width || insertHeight > rect.height)
                return false;

            if (left == null)
            {
                left = new ImagePackNode();
                left.rect = new RectInt(rect.x, rect.y, rect.width, insertHeight);
                right = new ImagePackNode();
                right.rect = new RectInt(rect.x, rect.y + insertHeight, rect.width, rect.height - insertHeight);
            }

            if (!left.InsertNotRoot(insert, padding))
            {
                return right.InsertNotRoot(insert, padding);
            }

            return true;
        }

        void AdjustSizeNotRoot(int oriWidth, int oriHeight, int deltaW, int deltaH, out int adjustx, out int adjusty)
        {
            adjustx = adjusty = 0;
            int adjustXleft = 0, adjustYleft = 0, adjustXRight = 0, adjustYRight = 0;
            if (imageId == -1)
            {
                if (rect.x + rect.width == oriWidth)
                {
                    rect.width += deltaW;
                    adjustx = deltaW;
                }
                if (rect.y + rect.height == oriHeight)
                {
                    rect.height += deltaH;
                    adjusty = deltaH;
                }
            }
            else
            {
                if (left != null)
                    left.AdjustSizeNotRoot(oriWidth, oriHeight, deltaW, deltaH, out adjustXleft, out adjustYleft);
                if (right != null)
                    right.AdjustSizeNotRoot(oriWidth, oriHeight, deltaW, deltaH, out adjustXRight, out adjustYRight);
                adjustx = Mathf.Max(adjustXleft, adjustXRight);
                rect.width += adjustx;
                adjusty = Mathf.Max(adjustYleft, adjustYRight);
                rect.height += adjusty;
            }
        }

        bool InsertNotRoot(ImagePacker.ImagePackRect insert, int padding)
        {
            int insertWidth = insert.rect.width + padding * 2;
            int insertHeight = insert.rect.height + padding * 2;
            if (insertWidth > rect.width || insertHeight > rect.height)
                return false;

            if (imageId == -1)
            {
                imageId = insert.index;
                var dw = rect.width - insertWidth;
                var dh = rect.height - insertHeight;
                left = new ImagePackNode();
                right = new ImagePackNode();
                if (dw > dh)
                {
                    left.rect = new RectInt(rect.x + insertWidth, rect.y, rect.width - insertWidth, rect.height);
                    right.rect = new RectInt(rect.x, rect.y + insertHeight, insertWidth, rect.height - insertHeight);
                }
                else
                {
                    left.rect = new RectInt(rect.x, rect.y + insertHeight, rect.width, rect.height - insertHeight);
                    right.rect = new RectInt(rect.x + insertWidth, rect.y, rect.width - insertWidth, insertHeight);
                }
            }
            else
            {
                if (!left.InsertNotRoot(insert, padding))
                    return right.InsertNotRoot(insert, padding);
            }
            return true;
        }
    }
}
